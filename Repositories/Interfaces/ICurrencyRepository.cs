using OPA_Pay.Models;

namespace OPA_Pay.Repositories.Interfaces
{
    public interface ICurrencyRepository
    {
        Task<List<Currency>> GetAllAsync();
        Task<Currency?> GetByIdAsync(int id);
        Task<Currency?> GetByCodeAsync(string code);
        Task AddAsync(Currency currency);
        Task UpdateAsync(Currency currency);
        Task SaveAsync();
    }
}