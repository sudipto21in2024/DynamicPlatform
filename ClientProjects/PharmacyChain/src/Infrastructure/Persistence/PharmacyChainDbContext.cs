using Microsoft.EntityFrameworkCore;
using Platform.Infrastructure.Data;
using PharmacyChain.Core.Domain.StoreContext;
using PharmacyChain.Core.Domain.InventoryContext;
using PharmacyChain.Core.Domain.SalesContext;
using PharmacyChain.Core.Domain.LogisticsContext;
using PharmacyChain.Core.Domain.PricingContext;
using PharmacyChain.Core.Domain.ProcurementContext;

namespace PharmacyChain.Infrastructure.Persistence
{
    public class PharmacyChainDbContext : PlatformDbContext
    {
        public PharmacyChainDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
        {
        }

        // StoreContext
        public DbSet<Store> Stores { get; set; }
        public DbSet<Staff> Staff { get; set; }

        // InventoryContext
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Batch> Batches { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }

        // SalesContext
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }

        // LogisticsContext
        public DbSet<OnlineOrder> OnlineOrders { get; set; }
        public DbSet<DeliveryPartner> DeliveryPartners { get; set; }
        public DbSet<DeliveryCoverageZone> DeliveryCoverageZones { get; set; }
        public DbSet<DeliveryAssignment> DeliveryAssignments { get; set; }

        // PricingContext
        public DbSet<DiscountRule> DiscountRules { get; set; }
        public DbSet<ApplicableScope> ApplicableScopes { get; set; }
        public DbSet<CouponCode> CouponCodes { get; set; }

        // ProcurementContext
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Basic relationship configurations can be added here
            // For now, EF Core conventions will handle most 1:N relationships based on property names
        }
    }
}
