using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;
using OPA_Pay.Models;
using OPA_Pay.Repositories.Interfaces;

namespace OPA_Pay.Repositories.Implementations
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public TransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Transaction>> GetAllAsync()
        {
            return await _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Transfer)
                .OrderByDescending(t => t.Id)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetByUserIdAsync(string userId)
        {
            return await _context.Transactions
                .Include(t => t.Account)
                    .ThenInclude(a => a.Currency)
                .Include(t => t.Transfer)
                .Where(t => t.Account.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Transaction?> GetByIdAsync(int id)
        {
            return await _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Transfer)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddAsync(Transaction entity)
        {
            await _context.Transactions.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public Task UpdateAsync(Transaction entity)
        {
            _context.Transactions.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var item = await GetByIdAsync(id);

            if (item != null)
            {
                _context.Transactions.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public Task SaveAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}