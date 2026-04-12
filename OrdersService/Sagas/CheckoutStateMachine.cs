using MassTransit;
using RetailPOS.Contracts;

namespace OrdersService.Sagas
{
    public class CheckoutStateMachine : MassTransitStateMachine<CheckoutSagaState>
    {
        public CheckoutStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => CheckoutInitiated, x => x.CorrelateBy((saga, context) => saga.OrderId == context.Message.OrderId).SelectId(context => NewId.NextGuid()));
            Event(() => PaymentProcessed, x => x.CorrelateBy((saga, context) => saga.OrderId == context.Message.OrderId));
            Event(() => StockDeducted, x => x.CorrelateBy((saga, context) => saga.OrderId == context.Message.OrderId));
            Event(() => StockDeductionFailed, x => x.CorrelateBy((saga, context) => saga.OrderId == context.Message.OrderId));
            Event(() => PaymentRefunded, x => x.CorrelateBy((saga, context) => saga.OrderId == context.Message.OrderId));

            Initially(
                When(CheckoutInitiated)
                    .Then(context => {
                        context.Saga.OrderId = context.Message.OrderId;
                        context.Saga.StoreId = context.Message.StoreId;
                        context.Saga.TotalAmount = context.Message.TotalAmount;
                        context.Saga.CustomerMobile = context.Message.CustomerMobile;
                        context.Saga.CreatedAt = DateTime.UtcNow;
                        context.Saga.Items = context.Message.Items.Select(i => new SagaOrderItem { ProductId = i.ProductId, Quantity = i.Quantity }).ToList();
                    })
                    .TransitionTo(PaymentProcessing)
                    .PublishAsync(context => context.Init<ProcessPaymentCommand>(new {
                        OrderId = context.Saga.OrderId,
                        Amount = context.Saga.TotalAmount,
                        CustomerMobile = context.Saga.CustomerMobile ?? "",
                        PaymentMode = "Standard"
                    }))
            );

            During(PaymentProcessing,
                When(PaymentProcessed)
                    .If(context => context.Message.Status == "Success",
                        binder => binder
                            .TransitionTo(StockDeductionProcessing)
                            .PublishAsync(context => context.Init<DeductStockCommand>(new {
                                OrderId = context.Saga.OrderId,
                                Items = context.Saga.Items!.Select(i => new { i.ProductId, i.Quantity }).ToList()
                            }))
                    )
                    .If(context => context.Message.Status != "Success",
                        binder => binder
                            .TransitionTo(Failed)
                            .Then(context => Console.WriteLine($"Checkout Failed for Order {context.Saga.OrderId}: Payment unsuccessful."))
                            .Finalize()
                    )
            );

            During(StockDeductionProcessing,
                When(StockDeducted)
                    .TransitionTo(Completed)
                    .PublishAsync(context => context.Init<FinalizeOrderCommand>(new { OrderId = context.Saga.OrderId }))
                    .PublishAsync(context => context.Init<OrderPlacedEvent>(new { 
                        OrderId = context.Saga.OrderId,
                        StoreId = context.Saga.StoreId,
                        TotalAmount = context.Saga.TotalAmount,
                        TaxAmount = 0.0m, // Tax is included in TotalAmount typically or can be calculated
                        Date = DateTime.UtcNow,
                        CustomerMobile = context.Saga.CustomerMobile,
                        Items = context.Saga.Items!.Select(i => new { i.ProductId, i.Quantity }).ToList()
                    }))
                    .Then(context => Console.WriteLine($"Checkout Completed for Order {context.Saga.OrderId}"))
                    .Finalize(),
                When(StockDeductionFailed)
                    .TransitionTo(Refunding)
                    .PublishAsync(context => context.Init<RefundPaymentCommand>(new {
                        OrderId = context.Saga.OrderId,
                        PaymentId = context.Saga.PaymentId ?? 0,
                        RefundAmount = context.Saga.TotalAmount,
                        Reason = "Stock deduction failed after payment."
                    }))
            );

            During(Refunding,
                When(PaymentRefunded)
                    .TransitionTo(Failed)
                    .Then(context => Console.WriteLine($"Order {context.Saga.OrderId} Refunded and Failed due to Stock issues."))
                    .Finalize()
            );

            SetCompletedWhenFinalized();
        }

        public State PaymentProcessing { get; private set; } = default!;
        public State StockDeductionProcessing { get; private set; } = default!;
        public State Refunding { get; private set; } = default!;
        public State Completed { get; private set; } = default!;
        public State Failed { get; private set; } = default!;

        public Event<CheckoutInitiatedEvent> CheckoutInitiated { get; private set; } = default!;
        public Event<PaymentProcessedEvent> PaymentProcessed { get; private set; } = default!;
        public Event<StockDeductedEvent> StockDeducted { get; private set; } = default!;
        public Event<StockDeductionFailedEvent> StockDeductionFailed { get; private set; } = default!;
        public Event<PaymentRefundedEvent> PaymentRefunded { get; private set; } = default!;
    }

    // Missing event contract for Refund confirmation
}
