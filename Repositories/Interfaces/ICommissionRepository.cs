using OPA_Pay.Models;

namespace OPA_Pay.Repositories.Interfaces
{
    public interface ICommissionRepository
    {
        Task<Commission?> GetActiveAsync();
        Task<List<Commission>> GetAllAsync();
        Task AddAsync(Commission commission);
        Task UpdateAsync(Commission commission);
        Task SaveAsync();
    }
}