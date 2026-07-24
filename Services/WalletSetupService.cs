using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;
using OPA_Pay.Helpers;
using OPA_Pay.Models;

namespace OPA_Pay.Services
{
    public interface IWalletSetupService
    {
        Task EnsureClientWalletsAsync(string userId);
        Task<int?> GetAccountIdForCurrencyAsync(string userId, int currencyId);
    }

    public class WalletSetupService : IWalletSetupService
    {
        /// <summary>USD=1, EUR=2, LBP=3 (seeded currencies)</summary>
        public static readonly int[] DefaultCurrencyIds = { 1, 2, 3 };

        private readonly ApplicationDbContext _context;

        public WalletSetupService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task EnsureClientWalletsAsync(string userId)
        {
            var existing = await _context.Accounts
                .Where(a => a.UserId == userId)
                .Select(a => a.CurrencyId)
                .ToListAsync();

            foreach (var currencyId in DefaultCurrencyIds)
            {
                if (existing.Contains(currencyId))
                    continue;

                await _context.Accounts.AddAsync(new Account
                {
                    UserId = userId,
                    CurrencyId = currencyId,
                    Balance = 0,
                    AccountNumber = SerialNumberGenerator.AccountNumber()
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int?> GetAccountIdForCurrencyAsync(string userId, int currencyId)
        {
            if (!DefaultCurrencyIds.Contains(currencyId))
                currencyId = 1;

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId && a.CurrencyId == currencyId);

            return account?.Id;
        }
    }
}
