using Microsoft.EntityFrameworkCore;
 
namespace Contoso.Api.Models
{
    public class ContosoDbContext : DbContext
    {
        public ContosoDbContext(DbContextOptions<ContosoDbContext> options) : base(options) { }
 
        public DbSet<User> Users { get; set; }
 
        public DbSet<Product> Products { get; set; }
       
 
        // public DbSet<OrderItem> OrderItems { get; set; }
 
        public DbSet<Order> Orders { get; set; }
 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .ToContainer("Users")
                .HasNoDiscriminator()
                .HasPartitionKey(u => u.Email);
 
            modelBuilder.Entity<Product>()
                .ToContainer("Products")
                .HasNoDiscriminator()
                .HasPartitionKey(p => p.Category);
 
            modelBuilder.Entity<Order>()
                .ToContainer("Orders")
                .HasNoDiscriminator()
                .HasPartitionKey(o => o.Id);
 
            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion<string>();

                modelBuilder.Entity<OrderItem>()
            .HasKey(oi => oi.Id);
 
        // Configure the one-to-many relationship
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId);
        }
    }
}