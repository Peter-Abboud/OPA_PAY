using OPA_Pay.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace OPA_Pay.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        // =========================
        // DB SETS
        // =========================

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Currency> Currencies { get; set; }

        public DbSet<Beneficiary> Beneficiaries { get; set; }

        public DbSet<Transfer> Transfers { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<Agent> AgentProfiles { get; set; }

        public DbSet<Commission> Commissions { get; set; }

        public DbSet<Receipt> Receipts { get; set; }

        public DbSet<Review> Reviews { get; set; }

        public DbSet<FundRequest> FundRequests { get; set; }


        // =========================
        // MODEL CONFIGURATION
        // =========================

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            // =====================================
            // ACCOUNT → USER
            // =====================================

            builder.Entity<Account>()
                .HasOne(a => a.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================
            // ACCOUNT → CURRENCY
            // =====================================

            builder.Entity<Account>()
                .HasOne(a => a.Currency)
                .WithMany(c => c.Accounts)
                .HasForeignKey(a => a.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================
            // BENEFICIARY → USER
            // =====================================

            builder.Entity<Beneficiary>()
                .HasOne(b => b.User)
                .WithMany(u => u.Beneficiaries)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================
            // TRANSFER → ACCOUNT
            // =====================================

            builder.Entity<Transfer>()
                .HasOne(t => t.Account)
                .WithMany(a => a.Transfers)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================
            // TRANSFER → BENEFICIARY
            // =====================================

            builder.Entity<Transfer>()
                .HasOne(t => t.Beneficiary)
                .WithMany(b => b.Transfers)
                .HasForeignKey(t => t.BeneficiaryId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================
            // TRANSACTION → ACCOUNT
            // =====================================

            builder.Entity<Transaction>()
                .HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================
            // TRANSACTION → TRANSFER
            // =====================================

            builder.Entity<Transaction>()
                .HasOne(t => t.Transfer)
                .WithOne(tr => tr.Transaction)
                .HasForeignKey<Transaction>(t => t.TransferId)
                .OnDelete(DeleteBehavior.NoAction);


            // =====================================
            // NOTIFICATION → USER
            // =====================================

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================
            // AGENT PROFILE → USER
            // =====================================

            builder.Entity<Agent>()
                .HasOne(a => a.User)
                .WithOne(u => u.AgentProfile)
                .HasForeignKey<Agent>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================
            // RECEIPT → TRANSFER
            // =====================================

            builder.Entity<Receipt>()
                .HasOne(r => r.Transfer)
                .WithOne(t => t.Receipt)
                .HasForeignKey<Receipt>(r => r.TransferId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================
            // FUND REQUEST → USER / ACCOUNT
            // =====================================

            builder.Entity<FundRequest>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FundRequest>()
                .HasOne(f => f.Account)
                .WithMany()
                .HasForeignKey(f => f.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FundRequest>()
                .Property(f => f.Amount)
                .HasColumnType("decimal(18,2)");

            // =====================================
            // REVIEW → USER
            // =====================================

            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================
            // REVIEW → AGENT PROFILE
            // =====================================

            builder.Entity<Review>()
                .HasOne(r => r.AgentProfile)
                .WithMany()
                .HasForeignKey(r => r.AgentProfileId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================
            // DECIMAL CONFIGURATION
            // =====================================

            builder.Entity<Account>()
                .Property(a => a.Balance)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Transfer>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Transfer>()
                .Property(t => t.Fee)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Currency>()
                .Property(c => c.ExchangeRate)
                .HasColumnType("decimal(18,4)");

            builder.Entity<Commission>()
                .Property(c => c.Percentage)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Commission>()
                .Property(c => c.FixedAmount)
                .HasColumnType("decimal(18,2)");



            // =====================================
            // SEED CURRENCIES
            // =====================================

            builder.Entity<Currency>().HasData(

                new Currency
                {
                    Id = 1,
                    Code = "USD",
                    Name = "US Dollar",
                    ExchangeRate = 1
                },

                new Currency
                {
                    Id = 2,
                    Code = "EUR",
                    Name = "Euro",
                    ExchangeRate = 0.92m
                },

                new Currency
                {
                    Id = 3,
                    Code = "LBP",
                    Name = "Lebanese Pound",
                    ExchangeRate = 89500
                }
            );



            // =====================================
            // SEED COMMISSION
            // =====================================

            builder.Entity<Commission>().HasData(

                new Commission
                {
                    Id = 1,
                    Percentage = 2,
                    FixedAmount = 1,
                    IsActive = true
                }
            );
        }
    }
}