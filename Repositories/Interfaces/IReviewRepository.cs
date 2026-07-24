using OPA_Pay.Models;

namespace OPA_Pay.Repositories.Interfaces
{
    public interface IReviewRepository
    {
        Task<List<Review>> GetAllAsync();
        Task<List<Review>> GetByAgentIdAsync(int agentId);
        Task AddAsync(Review review);
        Task SaveAsync();
    }
}