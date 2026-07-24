using OPA_Pay.Models;

namespace OPA_Pay.Repositories.Interfaces
{
    public interface IReceiptRepository
    {
        Task<List<Receipt>> GetAllAsync();
        Task<Receipt?> GetByTransferIdAsync(int transferId);
        Task<List<Receipt>> GetByUserIdAsync(string userId);
        Task AddAsync(Receipt receipt);
        Task SaveAsync();
    }
}