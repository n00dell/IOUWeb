using IOU.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IOU.Web.Data
{
    public class IOUWebContext : IdentityDbContext<ApplicationUser>
    {
        public IOUWebContext(DbContextOptions<IOUWebContext> options)
                : base(options)
            {
            }
        public DbSet<Student> Student { get; set; }
        
        public DbSet<Lender> Lender { get; set; }

        public DbSet<Notification> Notification { get; set; }
        public DbSet<ScheduledPayment> ScheduledPayment { get; set; }
        
        public DbSet<Debt> Debt { get; set; }

        public DbSet<Dispute> Dispute { get; set; }

        public DbSet<DisputeDetail> DisputeDetail { get; set; }

        public DbSet<SupportingDocument> SupportingDocument { get; set; }

        public DbSet<DebtEvidence> DebtEvidence { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<CreditReport> CreditReports { get; set; }

        public DbSet<CreditReportRequest> CreditReportRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Payment>(entity =>
            {
                // Proper indexing
                entity.HasIndex(p => p.CheckoutRequestID).IsUnique();
                entity.HasIndex(p => new { p.Status, p.UpdatedAt });

                entity.Property(p => p.UpdatedAt)
                    .IsConcurrencyToken();

                // Precision for amount
                entity.Property(p => p.Amount).HasPrecision(18, 2);

                // Relationships
                entity.HasOne(p => p.Debt)
                    .WithMany(d => d.Payments)
                    .HasForeignKey(p => p.DebtId)
                    .OnDelete(DeleteBehavior.Restrict); // Changed from Cascade

                entity.HasOne(p => p.ScheduledPayment)
                    .WithMany(s => s.Payments)
                    .HasForeignKey(p => p.ScheduledPaymentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Default values
                entity.Property(p => p.Status)
                    .HasDefaultValue(PaymentTransactionStatus.Pending);
            });

            modelBuilder.Model.GetEntityTypes()
       .Where(e => e.ClrType == typeof(Debt))
       .ToList()
       .ForEach(e => e.GetProperties()
           .Where(p => p.ClrType == typeof(decimal))
           .ToList()
           .ForEach(p => p.SetPrecision(18)));

            // Debt -> ScheduledPayments (installments)
            modelBuilder.Entity<Debt>()
                .HasMany(d => d.ScheduledPayments)
                .WithOne(p => p.Debt)
                .HasForeignKey(p => p.DebtId)
                .OnDelete(DeleteBehavior.Cascade);

            // Debt -> Payments (transactions)
            modelBuilder.Entity<Debt>()
                .HasMany(d => d.Payments)
                .WithOne(p => p.Debt)
                .HasForeignKey(p => p.DebtId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CreditReportRequest>()
        .HasIndex(r => new { r.StudentEmail, r.LenderUserId });

            modelBuilder.Entity<CreditReport>()
                .HasIndex(r => r.StudentUserId);
            modelBuilder.Entity<CreditReportRequest>()
        .HasOne(r => r.Student)
        .WithMany()
        .HasForeignKey(r => r.StudentUserId)
        .OnDelete(DeleteBehavior.Restrict); // Changed from Cascade to Restrict
            modelBuilder.Entity<CreditReportRequest>()
    .HasOne(c => c.CreditReport)
    .WithMany()
    .HasForeignKey(c => c.CreditReportId)
    .IsRequired(false);
            // Configure decimal precision for CreditReport
            modelBuilder.Entity<CreditReport>(e =>
            {
                e.Property(p => p.CreditScore).HasPrecision(18, 2);
                e.Property(p => p.PaymentCompletionRate).HasPrecision(5, 2);
                e.Property(p => p.AveragePaymentDelayDays).HasPrecision(5, 2);
                e.Property(p => p.TotalDebtObligations).HasPrecision(18, 2);
            });

            // Configure decimal precision for ScheduledPayment
            modelBuilder.Entity<ScheduledPayment>(e =>
            {
                e.Property(p => p.PaidAmount).HasPrecision(18, 2);
            });
            // ScheduledPayment -> Payments (installment payments)
            modelBuilder.Entity<ScheduledPayment>(entity =>
            {
                entity.HasOne(s => s.Debt)
                    .WithMany(d => d.ScheduledPayments)
                    .HasForeignKey(s => s.DebtId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Ensure proper precision
                entity.Property(s => s.Amount).HasPrecision(18, 2);
                entity.Property(s => s.PrincipalPortion).HasPrecision(18, 2);
                entity.Property(s => s.InterestPortion).HasPrecision(18, 2);
                entity.Property(s => s.LateFeesPortion).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Debt>(entity =>
            {
                entity.HasOne(d => d.Student)
                    .WithMany(s => s.Debts)
                    .HasForeignKey(d => d.StudentUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Lender)
                    .WithMany(l => l.IssuedDebts)
                    .HasForeignKey(d => d.LenderUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure cascade delete for Lender -> Debt relationship
            modelBuilder.Entity<Lender>()
                .HasMany(l => l.IssuedDebts)
                .WithOne(d => d.Lender)
                .HasForeignKey(d => d.LenderUserId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete

            modelBuilder.Entity<Student>()
        .HasOne(s => s.User)
        .WithOne(u => u.Student)
        .HasForeignKey<Student>(s => s.UserId)
        .OnDelete(DeleteBehavior.NoAction);

            // Configure one-to-one relationship between ApplicationUser and Lender
            modelBuilder.Entity<Lender>()
                .HasOne(l => l.User)
                .WithOne(u => u.Lender)
                .HasForeignKey<Lender>(l => l.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<DebtEvidence>(entity =>
            {
                entity.HasOne(d => d.Dispute)
                    .WithMany(d => d.LenderEvidence)
                    .HasForeignKey(d => d.DisputeId)
                    .OnDelete(DeleteBehavior.Restrict);  // Change from Cascade to Restrict
                
                entity.HasOne(d => d.Lender)
                    .WithMany(l => l.SubmittedEvidence)
                    .HasForeignKey(d => d.LenderUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Add this for Dispute relationships
            modelBuilder.Entity<Dispute>(entity =>
            {
                entity.HasOne(d => d.Debt)
                    .WithMany(d => d.Disputes)
                    .HasForeignKey(d => d.DebtId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            // Configure decimal precision for Debt
            modelBuilder.Entity<Debt>(entity =>
            {
                entity.Property(d => d.PrincipalAmount).HasPrecision(18, 2);
                entity.Property(d => d.CurrentBalance).HasPrecision(18, 2);
                entity.Property(d => d.InterestRate).HasPrecision(5, 2);
                entity.Property(d => d.AccumulatedInterest).HasPrecision(18, 2);
                entity.Property(d => d.LateFeeAmount).HasPrecision(18, 2);
                entity.Property(d => d.AccumulatedLateFees).HasPrecision(18, 2);
            });
            modelBuilder.Entity<Dispute>(entity =>
            {
                entity.HasMany(d => d.SupportingDocuments)
                      .WithOne(s => s.Dispute) // Use the navigation property in SupportingDocument
                      .HasForeignKey(s => s.DisputeId) // Use the existing DisputeId property
                      .OnDelete(DeleteBehavior.Cascade); // Cascade delete
            });

            // Configure Dispute -> DebtEvidence relationship
            modelBuilder.Entity<Dispute>(entity =>
            {
                entity.HasMany(d => d.LenderEvidence)
                      .WithOne(e => e.Dispute) // Use the navigation property in DebtEvidence
                      .HasForeignKey(e => e.DisputeId) // Use the existing DisputeId property
                      .OnDelete(DeleteBehavior.Restrict); // Restrict delete to avoid multiple cascade paths
            });

            // Configure decimal precision for DisputeDetail
            modelBuilder.Entity<DisputeDetail>(entity =>
            {
                entity.Property(d => d.RequestedReductionAmount).HasPrecision(18, 2);
            });

            // Configure decimal precision for ScheduledPayment (if exists)
            

        }


    }
}
