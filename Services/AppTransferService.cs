using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;

using OPA_Pay.Data;

using OPA_Pay.Helpers;

using OPA_Pay.Models;

using OPA_Pay.Repositories.Interfaces;



namespace OPA_Pay.Services

{

    public class AppTransferService : ITransferService

    {

        private const int UsdCurrencyId = 1;



        private readonly ApplicationDbContext _context;

        private readonly ICommissionRepository _commissionRepo;

        private readonly ICurrencyConversionService _currencyService;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IEmailService _email;



        public AppTransferService(

            ApplicationDbContext context,

            ICommissionRepository commissionRepo,

            ICurrencyConversionService currencyService,

            UserManager<ApplicationUser> userManager,

            IEmailService email)

        {

            _context = context;

            _commissionRepo = commissionRepo;

            _currencyService = currencyService;

            _userManager = userManager;

            _email = email;

        }



        public async Task<FeeEstimate?> EstimateFeeAsync(int accountId, decimal amount, string userId)

        {

            if (amount <= 0)

                return null;



            var account = await _context.Accounts

                .Include(a => a.Currency)

                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);



            if (account == null)

                return null;



            var (fee, fixedUsd, percentage) = await CalculateFeeAsync(amount, account.CurrencyId);

            return new FeeEstimate

            {

                Amount = amount,

                Fee = fee,

                Total = amount + fee,

                CurrencyCode = account.Currency?.Code ?? "USD",

                FixedFeeUsd = fixedUsd,

                Percentage = percentage

            };

        }



        public async Task<TransferResult> ExecuteTransferAsync(

            int accountId, int beneficiaryId, decimal amount, string userId)

        {

            var account = await _context.Accounts

                .Include(a => a.Currency)

                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);



            if (account == null)

                return Fail("Account not found.");



            var beneficiary = await _context.Beneficiaries

                .FirstOrDefaultAsync(b => b.Id == beneficiaryId && b.UserId == userId);



            if (beneficiary == null)

                return Fail("Beneficiary not found.");



            var (fee, _, _) = await CalculateFeeAsync(amount, account.CurrencyId);

            var totalDebit = amount + fee;

            var code = account.Currency?.Code ?? "";



            if (account.Balance < totalDebit)

                return Fail($"Insufficient balance. Overdraft is not available. Available: {account.Balance:N2} {code}, required: {totalDebit:N2} {code} (amount + fee).");



            account.Balance -= totalDebit;



            var transfer = new Transfer

            {

                Reference = SerialNumberGenerator.TransferReference(),

                AccountId = accountId,

                BeneficiaryId = beneficiaryId,

                Amount = amount,

                Fee = fee,

                Status = "Completed",

                TransferMethod = "Beneficiary",

                CreatedAt = DateTime.Now

            };



            await _context.Transfers.AddAsync(transfer);

            await _context.SaveChangesAsync();



            var recipientAccount = await _context.Accounts

                .Include(a => a.Currency)

                .FirstOrDefaultAsync(a => a.AccountNumber == beneficiary.AccountNumber);



            if (recipientAccount != null && recipientAccount.Id != accountId)

            {

                var convertedAmount = await _currencyService.ConvertBetweenCurrenciesAsync(

                    amount, account.CurrencyId, recipientAccount.CurrencyId);



                recipientAccount.Balance += convertedAmount;

            }



            await AddTransactionAsync(accountId, transfer.Id, amount, "Transfer");

            await AddReceiptAsync(transfer.Id);

            await AddNotificationAsync(userId, "Transfer Completed",

                $"Your transfer of {amount:N2} {code} to {beneficiary.FullName} was completed. Fee: {fee:N2} {code}. Reference: {transfer.Reference}");



            await _email.SendToUserAsync(userId, "OPA Pay — Transfer completed",

                EmailBody($"Transfer of <strong>{amount:N2} {code}</strong> to <strong>{beneficiary.FullName}</strong> completed. Ref: {transfer.Reference}"));



            await _context.SaveChangesAsync();



            return new TransferResult { Success = true, TransferId = transfer.Id };

        }



        public async Task<TransferResult> ExecuteMobileTransferAsync(

            int accountId, string recipientName, string mobileNumber, decimal amount, string userId)

        {

            var account = await _context.Accounts

                .Include(a => a.Currency)

                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);



            if (account == null)

                return Fail("Account not found.");



            recipientName = recipientName.Trim();

            var normalizedMobile = NormalizeMobile(mobileNumber);

            if (normalizedMobile.Length < 8)

                return Fail("Enter a valid mobile number (at least 8 digits).");



            var (fee, _, _) = await CalculateFeeAsync(amount, account.CurrencyId);

            var totalDebit = amount + fee;

            var code = account.Currency?.Code ?? "";



            if (account.Balance < totalDebit)

                return Fail($"Insufficient balance. Overdraft is not available. Available: {account.Balance:N2} {code}, required: {totalDebit:N2} {code} (amount + fee).");



            var beneficiary = await GetOrCreateMobileBeneficiaryAsync(userId, recipientName, normalizedMobile);



            account.Balance -= totalDebit;



            var pickupCode = SerialNumberGenerator.PickupCode();

            var transfer = new Transfer

            {

                Reference = SerialNumberGenerator.TransferReference(),

                PickupCode = pickupCode,

                AccountId = accountId,

                BeneficiaryId = beneficiary.Id,

                Amount = amount,

                Fee = fee,

                Status = "Pending Pickup",

                TransferMethod = "Mobile",

                CreatedAt = DateTime.Now

            };



            await _context.Transfers.AddAsync(transfer);

            await _context.SaveChangesAsync();



            await AddTransactionAsync(accountId, transfer.Id, amount, "Mobile Transfer");

            await AddReceiptAsync(transfer.Id);

            await AddNotificationAsync(userId, "Mobile Transfer Ready for Pickup",

                $"Send pickup code {pickupCode} to {recipientName}. Amount: {amount:N2} {code} (fee {fee:N2} {code}). Reference: {transfer.Reference}");



            await NotifyAgentsAsync("Mobile Pickup Pending",

                $"{recipientName} ({normalizedMobile}) — {amount:N2} {code}. Pickup code: {pickupCode}. Ref: {transfer.Reference}");



            await _email.SendToUserAsync(userId, "OPA Pay — Mobile transfer ready",

                EmailBody($"Pickup code: <strong>{pickupCode}</strong> for <strong>{recipientName}</strong>. Amount: {amount:N2} {code}."));

            await _email.SendToRoleAsync("Agent", "OPA Pay — Mobile pickup pending",

                EmailBody($"Pickup <strong>{pickupCode}</strong> — {recipientName}, {amount:N2} {code}. See Cash Operations."));



            await _context.SaveChangesAsync();



            return new TransferResult { Success = true, TransferId = transfer.Id };

        }



        private async Task<(decimal Fee, decimal FixedUsd, decimal Percentage)> CalculateFeeAsync(

            decimal amount, int accountCurrencyId)

        {

            var commission = await _commissionRepo.GetActiveAsync();

            if (commission == null)

                return (0, 0, 0);



            var percentFee = Math.Round(amount * commission.Percentage / 100, 2);

            var fixedUsd = commission.FixedAmount;

            var fixedInWallet = await _currencyService.ConvertBetweenCurrenciesAsync(

                fixedUsd, UsdCurrencyId, accountCurrencyId);



            return (Math.Round(percentFee + fixedInWallet, 2), fixedUsd, commission.Percentage);

        }



        private async Task<Beneficiary> GetOrCreateMobileBeneficiaryAsync(

            string userId, string recipientName, string normalizedMobile)

        {

            var existing = await _context.Beneficiaries

                .Where(b => b.UserId == userId && b.BankName == "Mobile Pickup")

                .ToListAsync();



            var match = existing.FirstOrDefault(b =>

                NormalizeMobile(b.MobileNumber) == normalizedMobile);



            if (match != null)

            {

                match.FullName = recipientName;

                match.MobileNumber = normalizedMobile;

                return match;

            }



            var beneficiary = new Beneficiary

            {

                FullName = recipientName,

                MobileNumber = normalizedMobile,

                AccountNumber = normalizedMobile,

                BankName = "Mobile Pickup",

                Country = "Local",

                UserId = userId

            };



            await _context.Beneficiaries.AddAsync(beneficiary);

            await _context.SaveChangesAsync();

            return beneficiary;

        }



        private static string NormalizeMobile(string mobile)

        {

            var digits = new string(mobile.Where(char.IsDigit).ToArray());

            if (digits.StartsWith("961") && digits.Length > 8)

                return "+" + digits;

            if (digits.Length == 8)

                return "+961" + digits;

            return digits.Length > 0 ? "+" + digits.TrimStart('+') : mobile.Trim();

        }



        private async Task NotifyAgentsAsync(string title, string message)

        {

            var agents = await _userManager.GetUsersInRoleAsync("Agent");

            foreach (var agent in agents)

            {

                await AddNotificationAsync(agent.Id, title, message);

            }

        }



        private async Task AddTransactionAsync(int accountId, int transferId, decimal amount, string type)

        {

            await _context.Transactions.AddAsync(new Transaction

            {

                AccountId = accountId,

                TransferId = transferId,

                Amount = amount,

                Type = type,

                CreatedAt = DateTime.Now

            });

        }



        private async Task AddReceiptAsync(int transferId)

        {

            await _context.Receipts.AddAsync(new Receipt

            {

                TransferId = transferId,

                ReceiptNumber = SerialNumberGenerator.ReceiptNumber(),

                CreatedAt = DateTime.Now

            });

        }



        private async Task AddNotificationAsync(string userId, string title, string message)

        {

            await _context.Notifications.AddAsync(new Notification

            {

                UserId = userId,

                Title = title,

                Message = message,

                IsRead = false,

                CreatedAt = DateTime.Now

            });

        }



        private static TransferResult Fail(string message)

            => new() { Success = false, ErrorMessage = message };



        private static string EmailBody(string content)

            => $"<div style='font-family:Segoe UI,sans-serif'><h2 style='color:#2563eb'>OPA Pay</h2><p>{content}</p></div>";

    }

}


