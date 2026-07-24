using OPA_Pay.Models;

namespace OPA_Pay.Repositories.Interfaces
{
    public interface IBeneficiaryRepository
    {
        Task<IEnumerable<Beneficiary>> GetAllAsync();
        Task<IEnumerable<Beneficiary>> GetByUserIdAsync(string userId);

        Task<Beneficiary?> GetByIdAsync(int id);

        Task AddAsync(Beneficiary beneficiary);

        Task UpdateAsync(Beneficiary beneficiary);

        Task DeleteAsync(int id);

        Task SaveAsync();
    }
}