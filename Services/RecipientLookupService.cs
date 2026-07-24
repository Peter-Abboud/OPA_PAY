using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;
using OPA_Pay.ViewModels;

namespace OPA_Pay.Services
{
    public interface IRecipientLookupService
    {
        Task<List<RecipientLookupResult>> SearchAsync(string term, string currentUserId);
        Task<List<RecipientLookupResult>> GetMobileRecipientsAsync(string currentUserId);
    }

    public class RecipientLookupService : IRecipientLookupService
    {
        private readonly ApplicationDbContext _context;

        public RecipientLookupService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RecipientLookupResult>> SearchAsync(string term, string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return new List<RecipientLookupResult>();

            term = term.Trim();
            var results = new Dictionary<string, RecipientLookupResult>(StringComparer.OrdinalIgnoreCase);

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.FullName.Contains(term) && u.Id != currentUserId)
                .Include(u => u.Accounts)
                .Take(15)
                .ToListAsync();

            foreach (var user in users)
            {
                Merge(results, user.FullName, "OPA User", opaAccounts: user.Accounts
                    .Select(a => a.AccountNumber)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList());
            }

            var beneficiaries = await _context.Beneficiaries
                .AsNoTracking()
                .Where(b => b.UserId == currentUserId && b.FullName.Contains(term))
                .Take(15)
                .ToListAsync();

            foreach (var b in beneficiaries)
            {
                Merge(results, b.FullName, "Saved Beneficiary",
                    opaAccounts: string.IsNullOrWhiteSpace(b.AccountNumber) ? null : new List<string> { b.AccountNumber },
                    bankNames: string.IsNullOrWhiteSpace(b.BankName) ? null : new List<string> { b.BankName },
                    countries: string.IsNullOrWhiteSpace(b.Country) ? null : new List<string> { b.Country },
                    mobiles: string.IsNullOrWhiteSpace(b.MobileNumber) ? null : new List<string> { b.MobileNumber });
            }

            var otherBeneficiaries = await _context.Beneficiaries
                .AsNoTracking()
                .Where(b => b.UserId != currentUserId && b.FullName.Contains(term))
                .Select(b => new { b.FullName, b.BankName, b.Country, b.AccountNumber, b.MobileNumber })
                .Take(10)
                .ToListAsync();

            foreach (var b in otherBeneficiaries)
            {
                Merge(results, b.FullName, "Directory",
                    opaAccounts: string.IsNullOrWhiteSpace(b.AccountNumber) ? null : new List<string> { b.AccountNumber },
                    bankNames: string.IsNullOrWhiteSpace(b.BankName) ? null : new List<string> { b.BankName },
                    countries: string.IsNullOrWhiteSpace(b.Country) ? null : new List<string> { b.Country },
                    mobiles: string.IsNullOrWhiteSpace(b.MobileNumber) ? null : new List<string> { b.MobileNumber });
            }

            return results.Values.OrderBy(r => r.FullName).Take(12).ToList();
        }

        public async Task<List<RecipientLookupResult>> GetMobileRecipientsAsync(string currentUserId)
        {
            var list = new List<RecipientLookupResult>();

            var beneficiaries = await _context.Beneficiaries
                .AsNoTracking()
                .Where(b => b.UserId == currentUserId)
                .OrderBy(b => b.FullName)
                .ToListAsync();

            foreach (var b in beneficiaries)
            {
                list.Add(new RecipientLookupResult
                {
                    FullName = b.FullName,
                    MobileNumbers = string.IsNullOrWhiteSpace(b.MobileNumber)
                        ? new List<string>()
                        : new List<string> { b.MobileNumber },
                    OpaAccountNumbers = string.IsNullOrWhiteSpace(b.AccountNumber)
                        ? new List<string>()
                        : new List<string> { b.AccountNumber },
                    BankNames = string.IsNullOrWhiteSpace(b.BankName) ? new List<string>() : new List<string> { b.BankName },
                    Countries = string.IsNullOrWhiteSpace(b.Country) ? new List<string>() : new List<string> { b.Country },
                    Source = "Beneficiary"
                });
            }

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id != currentUserId && !string.IsNullOrEmpty(u.PhoneNumber))
                .OrderBy(u => u.FullName)
                .Take(20)
                .ToListAsync();

            foreach (var u in users.Where(u => !list.Any(l => l.FullName.Equals(u.FullName, StringComparison.OrdinalIgnoreCase))))
            {
                list.Add(new RecipientLookupResult
                {
                    FullName = u.FullName,
                    MobileNumbers = string.IsNullOrWhiteSpace(u.PhoneNumber) ? new List<string>() : new List<string> { u.PhoneNumber! },
                    Source = "OPA User"
                });
            }

            return list;
        }

        private static void Merge(
            Dictionary<string, RecipientLookupResult> results,
            string fullName,
            string source,
            List<string>? opaAccounts = null,
            List<string>? bankNames = null,
            List<string>? countries = null,
            List<string>? mobiles = null)
        {
            if (!results.TryGetValue(fullName, out var entry))
            {
                entry = new RecipientLookupResult { FullName = fullName, Source = source };
                results[fullName] = entry;
            }

            if (opaAccounts != null)
                foreach (var x in opaAccounts.Where(x => !string.IsNullOrWhiteSpace(x) && !entry.OpaAccountNumbers.Contains(x)))
                    entry.OpaAccountNumbers.Add(x);

            if (bankNames != null)
                foreach (var x in bankNames.Where(x => !string.IsNullOrWhiteSpace(x) && !entry.BankNames.Contains(x)))
                    entry.BankNames.Add(x);

            if (countries != null)
                foreach (var x in countries.Where(x => !string.IsNullOrWhiteSpace(x) && !entry.Countries.Contains(x)))
                    entry.Countries.Add(x);

            if (mobiles != null)
                foreach (var x in mobiles.Where(x => !string.IsNullOrWhiteSpace(x) && !entry.MobileNumbers.Contains(x)))
                    entry.MobileNumbers.Add(x);
        }
    }
}
