using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;
using OPA_Pay.Models;
using OPA_Pay.Repositories.Interfaces;

namespace OPA_Pay.Repositories.Implementations
{
    public class AgentProfileRepository : IAgentProfileRepository
    {
        private readonly ApplicationDbContext _context;

        public AgentProfileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Agent>> GetAllAsync()
        {
            return await _context.AgentProfiles
                .Include(a => a.User)
                .ToListAsync();
        }

        public async Task<Agent?> GetByIdAsync(int id)
        {
            return await _context.AgentProfiles
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Agent?> GetByUserIdAsync(string userId)
        {
            return await _context.AgentProfiles
                .FirstOrDefaultAsync(a => a.UserId == userId);
        }

        public async Task AddAsync(Agent agent)
        {
            await _context.AgentProfiles.AddAsync(agent);
        }

        public async Task UpdateAsync(Agent agent)
        {
            var entry = _context.Entry(agent);
            if (entry.State == EntityState.Detached)
                _context.AgentProfiles.Update(agent);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
                _context.AgentProfiles.Remove(entity);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}