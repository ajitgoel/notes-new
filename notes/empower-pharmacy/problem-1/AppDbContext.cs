using Microsoft.EntityFrameworkCore;

namespace Problem1
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Prescription> Prescriptions => Set<Prescription>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Prescription DTO-like constraints in the database
            modelBuilder.Entity<Prescription>(entity =>
            {
                entity.Property(e => e.MedicationName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Status).HasDefaultValue("pending");
            });

            modelBuilder.Entity<Patient>(entity =>
            {
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.State).IsRequired();
            });
        }
    }
}
