using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;
using OPA_Pay.Models;
using OPA_Pay.Repositories.Interfaces;

namespace OPA_Pay.Repositories
{
    public class TransferRepository : ITransferRepository
    {
        private readonly ApplicationDbContext _context;

        public TransferRepository(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IEnumerable<Transfer>> GetAllAsync()
        {
            return await _context.Transfers
                .Include(t => t.Account)
                .Include(t => t.Beneficiary)
                .Include(t => t.Receipt)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transfer>> GetByUserIdAsync(string userId)
        {
            return await _context.Transfers
                .Include(t => t.Account)
                    .ThenInclude(a => a.Currency)
                .Include(t => t.Beneficiary)
                .Include(t => t.Receipt)
                .Where(t => t.Account.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }


        public async Task<Transfer?> GetByIdAsync(int id)
        {
            return await _context.Transfers
                .Include(t => t.Account)
                    .ThenInclude(a => a.Currency)
                .Include(t => t.Beneficiary)
                .Include(t => t.Receipt)
                .FirstOrDefaultAsync(t => t.Id == id);
        }


        public async Task AddAsync(Transfer transfer)
        {
            await _context.Transfers.AddAsync(transfer);
        }


        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}