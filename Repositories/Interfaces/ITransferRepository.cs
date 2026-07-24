using OPA_Pay.Models;

namespace OPA_Pay.Repositories.Interfaces
{
    public interface ITransferRepository
    {
        Task<IEnumerable<Transfer>> GetAllAsync();
        Task<IEnumerable<Transfer>> GetByUserIdAsync(string userId);

        Task<Transfer?> GetByIdAsync(int id);

        Task AddAsync(Transfer transfer);

        Task SaveAsync();
    }
}