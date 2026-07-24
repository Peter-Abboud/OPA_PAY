namespace OPA_Pay.Services
{
    public interface ICurrencyConversionService
    {
        decimal Convert(decimal amount, decimal fromRate, decimal toRate);
        Task<decimal> ConvertBetweenCurrenciesAsync(decimal amount, int fromCurrencyId, int toCurrencyId);
    }
}
