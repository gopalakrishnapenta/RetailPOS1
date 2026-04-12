using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReturnsService.Sagas;

namespace ReturnsService.Data
{
    public class ReturnsSagaDbContext : SagaDbContext
    {
        public ReturnsSagaDbContext(DbContextOptions<ReturnsSagaDbContext> options)
            : base(options)
        {
        }

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new ReturnSagaStateMap(); }
        }
    }

    public class ReturnSagaStateMap : SagaClassMap<ReturnSagaState>
    {
        protected override void Configure(EntityTypeBuilder<ReturnSagaState> entity, ModelBuilder modelBuilder)
        {
            entity.Property(x => x.CurrentState).HasMaxLength(64);
            entity.Property(x => x.CustomerMobile).HasMaxLength(20);
        }
    }
}
