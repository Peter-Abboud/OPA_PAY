using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;

namespace OPA_Pay.Services
{
    public class CurrencyConversionService : ICurrencyConversionService
    {
        private readonly ApplicationDbContext _context;

        public CurrencyConversionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public decimal Convert(decimal amount, decimal fromRate, decimal toRate)
        {
            if (fromRate <= 0 || toRate <= 0)
                return amount;

            var amountInBase = amount / fromRate;
            return Math.Round(amountInBase * toRate, 2);
        }

        public async Task<decimal> ConvertBetweenCurrenciesAsync(decimal amount, int fromCurrencyId, int toCurrencyId)
        {
            if (fromCurrencyId == toCurrencyId)
                return amount;

            var currencies = await _context.Currencies
                .Where(c => c.Id == fromCurrencyId || c.Id == toCurrencyId)
                .ToListAsync();

            var from = currencies.FirstOrDefault(c => c.Id == fromCurrencyId);
            var to = currencies.FirstOrDefault(c => c.Id == toCurrencyId);

            if (from == null || to == null)
                return amount;

            return Convert(amount, from.ExchangeRate, to.ExchangeRate);
        }
    }
}
