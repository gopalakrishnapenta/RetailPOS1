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

            Schedule(() => CheckoutTimeout, x => x.TimeoutTokenId, x => {
                x.Delay = TimeSpan.FromMinutes(5);
                x.Received = e => e.CorrelateBy((saga, context) => saga.OrderId == context.Message.OrderId);
            });

            Initially(
                When(CheckoutInitiated)
                    .Then(context => {
                        Console.WriteLine($"[SAGA DIAGNOSTIC] Starting Saga for Order {context.Message.OrderId} via CheckoutInitiated");
                        context.Saga.OrderId = context.Message.OrderId;
                        context.Saga.StoreId = context.Message.StoreId;
                        context.Saga.CashierId = context.Message.CashierId;
                        context.Saga.TotalAmount = context.Message.TotalAmount;
                        context.Saga.TaxAmount = context.Message.TaxAmount;
                        context.Saga.CustomerMobile = context.Message.CustomerMobile;
                        context.Saga.CreatedAt = DateTime.UtcNow;
                        context.Saga.Items = context.Message.Items.Select(i => new SagaOrderItem { ProductId = i.ProductId, Quantity = i.Quantity }).ToList();
                    })
                    .Schedule(CheckoutTimeout, context => context.Init<CheckoutTimeout>(new { OrderId = context.Saga.OrderId }))
                    .TransitionTo(PaymentProcessing),
                When(PaymentProcessed)
                    .Then(context => {
                        Console.WriteLine($"[SAGA DIAGNOSTIC] Starting Saga for Order {context.Message.OrderId} via PaymentProcessed (Early Event)");
                        context.Saga.OrderId = context.Message.OrderId;
                        context.Saga.PaymentId = context.Message.PaymentId;
                        context.Saga.PaymentStatus = context.Message.Status;
                        context.Saga.CreatedAt = DateTime.UtcNow;
                    })
                    .Schedule(CheckoutTimeout, context => context.Init<CheckoutTimeout>(new { OrderId = context.Saga.OrderId }))
                    .TransitionTo(WaitingForInitiation)
            );

            During(PaymentProcessing,
                When(PaymentProcessed)
                    .Then(context => Console.WriteLine($"[SAGA DIAGNOSTIC] Received PaymentProcessed for Order {context.Saga.OrderId}. Status: {context.Message.Status}"))
                    .If(context => context.Message.Status == "Success",
                        binder => binder
                            .Then(context => {
                                context.Saga.PaymentId = context.Message.PaymentId;
                                context.Saga.PaymentStatus = "Success";
                            })
                            .TransitionTo(StockDeductionProcessing)
                            .Then(context => Console.WriteLine($"[SAGA DIAGNOSTIC] Transitioned to StockDeductionProcessing for Order {context.Saga.OrderId}"))
                            .PublishAsync(context => context.Init<DeductStockCommand>(new {
                                OrderId = context.Saga.OrderId,
                                Items = context.Saga.Items!.Select(i => new { i.ProductId, i.Quantity }).ToList()
                            }))
                    )
                    .If(context => context.Message.Status != "Success",
                        binder => binder
                            .Unschedule(CheckoutTimeout)
                            .TransitionTo(Failed)
                            .Then(context => Console.WriteLine($"Checkout Failed for Order {context.Saga.OrderId}: Payment unsuccessful."))
                            .Finalize()
                    )
            );

            During(WaitingForInitiation,
                When(CheckoutInitiated)
                    .Then(context => {
                        Console.WriteLine($"[SAGA DIAGNOSTIC] Received CheckoutInitiated for Early-Started Saga (Order {context.Saga.OrderId})");
                        context.Saga.StoreId = context.Message.StoreId;
                        context.Saga.CashierId = context.Message.CashierId;
                        context.Saga.TotalAmount = context.Message.TotalAmount;
                        context.Saga.TaxAmount = context.Message.TaxAmount;
                        context.Saga.CustomerMobile = context.Message.CustomerMobile;
                        context.Saga.Items = context.Message.Items.Select(i => new SagaOrderItem { ProductId = i.ProductId, Quantity = i.Quantity }).ToList();
                    })
                    .If(context => context.Saga.PaymentStatus == "Success",
                        binder => binder
                            .TransitionTo(StockDeductionProcessing)
                            .Then(context => Console.WriteLine($"[SAGA DIAGNOSTIC] Payment already successful. Transitioning to StockDeductionProcessing for Order {context.Saga.OrderId}"))
                            .PublishAsync(context => context.Init<DeductStockCommand>(new {
                                OrderId = context.Saga.OrderId,
                                Items = context.Saga.Items!.Select(i => new { i.ProductId, i.Quantity }).ToList()
                            }))
                    )
            );

            During(StockDeductionProcessing,
                When(StockDeducted)
                    .Then(context => Console.WriteLine($"[SAGA DIAGNOSTIC] Stock Deducted for Order {context.Saga.OrderId}. Finalizing..."))
                    .Unschedule(CheckoutTimeout)
                    .TransitionTo(Completed)
                    .PublishAsync(context => context.Init<FinalizeOrderCommand>(new { OrderId = context.Saga.OrderId }))
                    .Then(context => Console.WriteLine($"[SAGA DIAGNOSTIC] Published FinalizeOrderCommand for Order {context.Saga.OrderId}"))
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
                    .Unschedule(CheckoutTimeout)
                    .TransitionTo(Failed)
                    .Then(context => Console.WriteLine($"Order {context.Saga.OrderId} Refunded and Failed due to Stock issues."))
                    .Finalize()
            );

            DuringAny(
                When(CheckoutTimeout.Received)
                    .Then(context => Console.WriteLine($"Checkout TIMEOUT reached for Order {context.Saga.OrderId}. Current State: {context.Saga.CurrentState}"))
                    .If(context => context.Saga.CurrentState == nameof(StockDeductionProcessing),
                        binder => binder
                            .PublishAsync(context => context.Init<RefundPaymentCommand>(new {
                                OrderId = context.Saga.OrderId,
                                PaymentId = context.Saga.PaymentId ?? 0,
                                RefundAmount = context.Saga.TotalAmount,
                                Reason = "Checkout Timeout after payment was processed."
                            }))
                            .TransitionTo(Refunding)
                    )
                    .If(context => context.Saga.CurrentState == nameof(PaymentProcessing),
                        binder => binder
                            .TransitionTo(Failed)
                            .Finalize()
                    )
            );

            SetCompletedWhenFinalized();
        }

        public State PaymentProcessing { get; private set; } = default!;
        public State WaitingForInitiation { get; private set; } = default!;
        public State StockDeductionProcessing { get; private set; } = default!;
        public State Refunding { get; private set; } = default!;
        public State Completed { get; private set; } = default!;
        public State Failed { get; private set; } = default!;

        public Event<CheckoutInitiatedEvent> CheckoutInitiated { get; private set; } = default!;
        public Event<PaymentProcessedEvent> PaymentProcessed { get; private set; } = default!;
        public Event<StockDeductedEvent> StockDeducted { get; private set; } = default!;
        public Event<StockDeductionFailedEvent> StockDeductionFailed { get; private set; } = default!;
        public Event<PaymentRefundedEvent> PaymentRefunded { get; private set; } = default!;

        public Schedule<CheckoutSagaState, CheckoutTimeout> CheckoutTimeout { get; private set; } = default!;
    }

    // Missing event contract for Refund confirmation
}
