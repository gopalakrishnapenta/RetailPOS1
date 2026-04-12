using MassTransit;
using RetailPOS.Contracts;

namespace ReturnsService.Sagas
{
    public class ReturnStateMachine : MassTransitStateMachine<ReturnSagaState>
    {
        public ReturnStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => ReturnInitiated, x => x.CorrelateBy((saga, context) => saga.ReturnId == context.Message.ServiceReturnId).SelectId(context => NewId.NextGuid()));
            Event(() => ReturnApproved, x => x.CorrelateBy((saga, context) => saga.ReturnId == context.Message.ReturnId));
            Event(() => ReturnRejected, x => x.CorrelateBy((saga, context) => saga.ReturnId == context.Message.ReturnId));
            Event(() => PaymentRefunded, x => x.CorrelateBy((saga, context) => saga.ReturnId == context.Message.ReturnId));
            Event(() => StockRestocked, x => x.CorrelateBy((saga, context) => saga.OrderId == context.Message.OrderId));

            Initially(
                When(ReturnInitiated)
                    .Then(context => {
                        context.Saga.ReturnId = context.Message.ServiceReturnId;
                        context.Saga.OrderId = context.Message.OrderId;
                        context.Saga.StoreId = context.Message.StoreId;
                        context.Saga.CreatedAt = DateTime.UtcNow;
                        
                        // Capture reporting data
                        var item = context.Message.Items.FirstOrDefault();
                        if (item != null)
                        {
                            context.Saga.ProductId = item.ProductId;
                            context.Saga.Quantity = item.Quantity;
                            // Estimate total refund from first item if not provided as a whole
                            context.Saga.RefundAmount = context.Message.Items.Sum(i => i.Quantity * 10); // Calculation placeholder or actual sync?
                        }
                        context.Saga.CustomerMobile = context.Message.CustomerMobile; 
                    })
                    .TransitionTo(AwaitingApproval)
            );

            During(AwaitingApproval,
                When(ReturnApproved)
                    .TransitionTo(Refunding)
                    .PublishAsync(context => context.Init<RefundPaymentCommand>(new {
                        OrderId = context.Saga.OrderId,
                        ReturnId = context.Saga.ReturnId,
                        RefundAmount = context.Saga.RefundAmount,
                        Reason = $"Return Approved: {context.Message.Note}"
                    })),
                When(ReturnRejected)
                    .TransitionTo(Rejected)
                    .PublishAsync(context => context.Init<FinalizeReturnCommand>(new {
                        ReturnId = context.Saga.ReturnId,
                        NewStatus = "Rejected"
                    }))
                    .Finalize()
            );

            During(Refunding,
                When(PaymentRefunded)
                    .TransitionTo(Restocking)
                    .PublishAsync(context => context.Init<RestockItemCommand>(new {
                        OrderId = context.Saga.OrderId,
                        Items = new[] { new { ProductId = context.Saga.ProductId, Quantity = context.Saga.Quantity } }.ToList()
                    }))
            );

            During(Restocking,
                When(StockRestocked)
                    .TransitionTo(Completed)
                    .PublishAsync(context => context.Init<FinalizeReturnCommand>(new {
                        ReturnId = context.Saga.ReturnId,
                        NewStatus = "Refunded"
                    }))
                    .PublishAsync(context => context.Init<OrderReturnedEvent>(new {
                        OrderId = context.Saga.OrderId,
                        ReturnId = context.Saga.ReturnId,
                        StoreId = context.Saga.StoreId,
                        RefundAmount = context.Saga.RefundAmount,
                        CustomerMobile = context.Saga.CustomerMobile,
                        Items = new[] { new { ProductId = context.Saga.ProductId, Quantity = context.Saga.Quantity } }.ToList()
                    }))
                    .Finalize()
            );

            SetCompletedWhenFinalized();
        }

        public State AwaitingApproval { get; private set; } = default!;
        public State Refunding { get; private set; } = default!;
        public State Restocking { get; private set; } = default!;
        public State Completed { get; private set; } = default!;
        public State Rejected { get; private set; } = default!;

        public Event<ReturnInitiatedEvent> ReturnInitiated { get; private set; } = default!;
        public Event<ReturnApprovedEvent> ReturnApproved { get; private set; } = default!;
        public Event<ReturnRejectedEvent> ReturnRejected { get; private set; } = default!;
        public Event<PaymentRefundedEvent> PaymentRefunded { get; private set; } = default!;
        public Event<StockRestockedEvent> StockRestocked { get; private set; } = default!;
    }
}
