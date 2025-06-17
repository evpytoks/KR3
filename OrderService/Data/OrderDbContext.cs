using System;
using Microsoft.EntityFrameworkCore;


namespace OrderService.Data;

public class OrderDbContext : DbContext
{
	public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
	}

    public DbSet<Order> Orders { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(20, 2);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.Error).IsRequired();
            entity.HasIndex(e => e.ProcessedUtc);
        });
    }
}

