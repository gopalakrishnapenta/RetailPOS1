using MassTransit;
using RetailPOS.Contracts;

namespace AdminService.Sagas
{
    public class OnboardingStateMachine : MassTransitStateMachine<OnboardingSagaState>
    {
        public OnboardingStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => UserRegistered, x => x.CorrelateBy((saga, context) => saga.UserId == context.Message.UserId).SelectId(context => NewId.NextGuid()));
            Event(() => ProfileCreated, x => x.CorrelateBy((saga, context) => saga.UserId == context.Message.UserId));

            Initially(
                When(UserRegistered)
                    .Then(context => {
                        context.Saga.UserId = context.Message.UserId;
                        context.Saga.Email = context.Message.Email;
                        context.Saga.FullName = context.Message.FullName;
                        context.Saga.RoleName = context.Message.RoleName;
                        context.Saga.StoreId = context.Message.StoreId;
                        context.Saga.CreatedAt = DateTime.UtcNow;
                    })
                    .TransitionTo(ProfileCreationPending)
                    .PublishAsync(context => context.Init<CreateStaffProfileCommand>(new {
                        UserId = context.Saga.UserId,
                        Email = context.Saga.Email,
                        FullName = context.Saga.FullName,
                        RoleName = context.Saga.RoleName,
                        StoreId = context.Saga.StoreId
                    }))
            );

            During(ProfileCreationPending,
                When(ProfileCreated)
                    .TransitionTo(Completed)
                    .Then(context => Console.WriteLine($"Onboarding Completed for User {context.Saga.Email}"))
                    .Finalize()
            );

            SetCompletedWhenFinalized();
        }

        public State ProfileCreationPending { get; private set; } = default!;
        public State Completed { get; private set; } = default!;

        public Event<UserRegisteredEvent> UserRegistered { get; private set; } = default!;
        public Event<StaffProfileCreatedEvent> ProfileCreated { get; private set; } = default!;
    }
}
