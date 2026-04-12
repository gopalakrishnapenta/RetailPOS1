using MassTransit;

namespace AdminService.Sagas
{
    public class OnboardingSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = default!;

        public int UserId { get; set; }
        public string Email { get; set; } = default!;
        public string? FullName { get; set; }
        public string? RoleName { get; set; }
        public int? StoreId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
