using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AdminService.Sagas;

namespace AdminService.Data
{
    public class AdminSagaDbContext : SagaDbContext
    {
        public AdminSagaDbContext(DbContextOptions<AdminSagaDbContext> options)
            : base(options)
        {
        }

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new OnboardingSagaStateMap(); }
        }
    }

    public class OnboardingSagaStateMap : SagaClassMap<OnboardingSagaState>
    {
        protected override void Configure(EntityTypeBuilder<OnboardingSagaState> entity, ModelBuilder modelBuilder)
        {
            entity.Property(x => x.CurrentState).HasMaxLength(64);
            entity.Property(x => x.Email).HasMaxLength(256);
        }
    }
}
