using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;
using OPA_Pay.Models;
using OPA_Pay.Repositories.Interfaces;

namespace OPA_Pay.Repositories.Implementations
{
    public class ReceiptRepository : IReceiptRepository
    {
        private readonly ApplicationDbContext _context;

        public ReceiptRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Receipt>> GetAllAsync()
        {
            return await _context.Receipts
                .Include(r => r.Transfer)
                .ToListAsync();
        }

        public async Task<Receipt?> GetByTransferIdAsync(int transferId)
        {
            return await _context.Receipts
                .Include(r => r.Transfer)
                .FirstOrDefaultAsync(r => r.TransferId == transferId);
        }

        public async Task<List<Receipt>> GetByUserIdAsync(string userId)
        {
            return await _context.Receipts
                .Include(r => r.Transfer)
                    .ThenInclude(t => t!.Beneficiary)
                .Include(r => r.Transfer)
                    .ThenInclude(t => t!.Account)
                        .ThenInclude(a => a.Currency)
                .Where(r => r.Transfer != null && r.Transfer.Account.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .ToListAsync();
        }

        public async Task AddAsync(Receipt receipt)
        {
            await _context.Receipts.AddAsync(receipt);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}