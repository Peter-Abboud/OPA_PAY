using OPA_Pay.Models;

namespace OPA_Pay.Repositories.Interfaces
{
    public interface ITransactionRepository
    {
        Task<List<Transaction>> GetAllAsync();
        Task<List<Transaction>> GetByUserIdAsync(string userId);

        Task<Transaction?> GetByIdAsync(int id);

        Task AddAsync(Transaction entity);

        Task UpdateAsync(Transaction entity);

        Task DeleteAsync(int id);

        Task SaveAsync();
    }
}