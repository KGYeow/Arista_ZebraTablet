using Microsoft.EntityFrameworkCore;

namespace Arista_ZebraTablet.Shared.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public virtual DbSet<ScannedBarcode> ScannedBarcodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ScannedBarcode>(entity =>
            {
                entity.ToTable("ScannedBarcode");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Value).IsRequired().HasMaxLength(255);
                entity.Property(e => e.BarcodeType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ScannedTime).IsRequired();
            });
        }
    }
}