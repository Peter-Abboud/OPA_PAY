using Microsoft.EntityFrameworkCore;
using OPA_Pay.Data;
using OPA_Pay.Models;
using OPA_Pay.Repositories.Interfaces;

namespace OPA_Pay.Repositories
{
    public class BeneficiaryRepository : IBeneficiaryRepository
    {
        private readonly ApplicationDbContext _context;

        public BeneficiaryRepository(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IEnumerable<Beneficiary>> GetAllAsync()
        {
            return await _context.Beneficiaries
                .Include(b => b.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<Beneficiary>> GetByUserIdAsync(string userId)
        {
            return await _context.Beneficiaries
                .Where(b => b.UserId == userId)
                .ToListAsync();
        }


        public async Task<Beneficiary?> GetByIdAsync(int id)
        {
            return await _context.Beneficiaries
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);
        }


        public async Task AddAsync(Beneficiary beneficiary)
        {
            await _context.Beneficiaries.AddAsync(beneficiary);
        }


        public async Task UpdateAsync(Beneficiary beneficiary)
        {
            _context.Beneficiaries.Update(beneficiary);

            await Task.CompletedTask;
        }


        public async Task DeleteAsync(int id)
        {
            var beneficiary = await GetByIdAsync(id);

            if (beneficiary != null)
            {
                _context.Beneficiaries.Remove(beneficiary);
            }
        }


        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}