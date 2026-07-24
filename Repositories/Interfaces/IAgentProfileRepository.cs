using OPA_Pay.Models;

namespace OPA_Pay.Repositories.Interfaces
{
    public interface IAgentProfileRepository
    {
        Task<List<Agent>> GetAllAsync();
        Task<Agent?> GetByIdAsync(int id);
        Task<Agent?> GetByUserIdAsync(string userId);
        Task AddAsync(Agent agent);
        Task UpdateAsync(Agent agent);
        Task DeleteAsync(int id);
        Task SaveAsync();
    }
}