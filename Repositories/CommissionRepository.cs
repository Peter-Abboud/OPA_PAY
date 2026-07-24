using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;
using OPA_Pay.Models;
using OPA_Pay.Repositories.Interfaces;

namespace OPA_Pay.Repositories.Implementations
{
    public class CommissionRepository : ICommissionRepository
    {
        private readonly ApplicationDbContext _context;

        public CommissionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Commission>> GetAllAsync()
        {
            return await _context.Commissions.ToListAsync();
        }

        public async Task<Commission?> GetActiveAsync()
        {
            return await _context.Commissions
                .FirstOrDefaultAsync(c => c.IsActive);
        }

        public async Task AddAsync(Commission commission)
        {
            await _context.Commissions.AddAsync(commission);
        }

        public async Task UpdateAsync(Commission commission)
        {
            _context.Commissions.Update(commission);
            await Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}