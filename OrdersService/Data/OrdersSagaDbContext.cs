using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrdersService.Sagas;

namespace OrdersService.Data
{
    public class OrdersSagaDbContext : SagaDbContext
    {
        public OrdersSagaDbContext(DbContextOptions<OrdersSagaDbContext> options)
            : base(options)
        {
        }

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new CheckoutSagaStateMap(); }
        }
    }

    public class CheckoutSagaStateMap : SagaClassMap<CheckoutSagaState>
    {
        protected override void Configure(EntityTypeBuilder<CheckoutSagaState> entity, ModelBuilder modelBuilder)
        {
            entity.Property(x => x.CurrentState).HasMaxLength(64);
            entity.Property(x => x.CustomerMobile).HasMaxLength(15);
            
            // Map the items list as JSON or a separate table. 
            // For simplicity in SQL Server 2019+, we'll use string conversion or just ignore for now if not strictly needed in the state.
            // Actually, we need it to deduct stock.
            entity.Property(x => x.Items)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<SagaOrderItem>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                );
        }
    }
}
