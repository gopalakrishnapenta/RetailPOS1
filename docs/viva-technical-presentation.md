# RetailPOS Technical Presentation Report (Viva)

## Title
RetailPOS: Event-Driven Microservices Point-of-Sale Platform

## Abstract
This project implements a retail point-of-sale system using microservices. It separates authentication, administration, catalog, orders, payments, returns, and notifications into independent services. Each service owns its own SQL Server database. Communication between services is handled using RabbitMQ and MassTransit, while clients interact through an API Gateway (Ocelot). Security is enforced using JWT tokens with fine-grained permission claims, enabling multi-tenant access control across stores.

## Problem Statement
Retail POS systems require secure access, store-level data isolation, fast billing, inventory accuracy, and operational reporting. A monolithic system becomes harder to scale and maintain as these concerns grow. This project addresses that by designing an event-driven microservices architecture with clear domain boundaries and asynchronous communication.

## Objectives
1. Build a secure multi-tenant POS backend with role and permission-based authorization.
2. Separate business domains into independent, scalable services.
3. Use asynchronous events for cross-service synchronization.
4. Provide real-time notifications for critical business events.
5. Keep data ownership isolated per service.

## System Architecture (HLD)
The system consists of:
1. ApiGateway (Ocelot): Single entry point for all client requests.
2. IdentityService: Authentication, OTP login, JWT issuance, refresh tokens, roles, permissions.
3. AdminService: Store management, staff assignment, categories, inventory adjustments, dashboard projections.
4. CatalogService: Products and categories, stock updates based on events.
5. OrdersService: Bills, bill items, customers, order lifecycle.
6. PaymentService: Payment capture and verification, payment completion events.
7. ReturnsService: Return initiation, approval, and return completion events.
8. NotificationService: Email, SMS, and SignalR real-time notifications.
9. RabbitMQ + MassTransit: Async event bus between services.
10. SQL Server: One database per service.

Diagrams:
1. HLD diagram: `C:\Users\Gopala Krishna\Desktop\Project\docs\hld-diagram.md`
2. RabbitMQ flow: `C:\Users\Gopala Krishna\Desktop\Project\docs\rabbitmq-flow.md`
3. ER diagram: `C:\Users\Gopala Krishna\Desktop\Project\docs\er-diagram.md`

## Tech Stack
1. Backend: ASP.NET Core (C#)
2. API Gateway: Ocelot
3. Messaging: RabbitMQ + MassTransit
4. Database: SQL Server (per service)
5. Authentication: JWT + refresh tokens
6. Authorization: Permission-based policies
7. Real-time: SignalR (NotificationService)
8. Logging: Serilog

## Core Services and Responsibilities

### ApiGateway
1. Routes `/gateway/...` endpoints to the correct service.
2. Provides a unified API surface for clients.

### IdentityService
1. Login with OTP and email verification.
2. JWT generation with claims for role, permissions, and store context.
3. Refresh token rotation.
4. Manages roles, permissions, and store mappings.

### AdminService
1. Store management (create/update/soft delete/restore).
2. Staff assignment and sync.
3. Category management and inventory adjustments.
4. Dashboard projections from order/return events.

### CatalogService
1. Product and category management.
2. Stock updates on `OrderPlacedEvent`, `OrderReturnedEvent`, and `StockAdjustedEvent`.

### OrdersService
1. Bill lifecycle: draft, hold, pending payment, finalized.
2. Customer management.
3. Publishes `OrderPlacedEvent` only after payment confirmation.

### PaymentService
1. Payment processing and Razorpay integration.
2. Publishes `PaymentProcessedEvent` on success.

### ReturnsService
1. Return initiation, approval, rejection.
2. Publishes `ReturnInitiatedEvent` and `OrderReturnedEvent`.

### NotificationService
1. Consumes user, order, return, and stock events.
2. Sends email/SMS/SignalR notifications.

## Authentication and Authorization
1. IdentityService validates user credentials and OTP.
2. JWT includes user identity, role, store id, and permission claims.
3. Each service validates the JWT locally using `AddJwtBearer`.
4. Authorization is permission-based using dynamic policies.

## Event-Driven Design
RabbitMQ carries events between services. Important flows:
1. PaymentService → `PaymentProcessedEvent` → OrdersService.
2. OrdersService → `OrderPlacedEvent` → CatalogService, AdminService, NotificationService.
3. ReturnsService → `ReturnInitiatedEvent` → OrdersService, NotificationService.
4. ReturnsService → `OrderReturnedEvent` → OrdersService, CatalogService, AdminService, NotificationService.
5. AdminService → `StockAdjustedEvent` → CatalogService, NotificationService.
6. AdminService → Store and Category events → IdentityService and CatalogService.
7. IdentityService → `UserRegisteredEvent` → AdminService, NotificationService.

## Database Design (ER Summary)
Each service has its own schema. Key entities:
1. Identity: User, Role, Permission, UserStoreRole, RolePermission, Store.
2. Admin: Store, Category, InventoryAdjustment, StaffMember, SyncedOrder, SyncedReturn.
3. Catalog: Product, Category.
4. Orders: Bill, BillItem, Customer.
5. Payments: Payment.
6. Returns: Return.
7. Notifications: Notification.

## Multi-Tenancy
1. Each request carries StoreId in JWT claims.
2. EF Core query filters limit data to the user’s store.
3. Admin users can access global data.

## Key Workflows

### Staff Onboarding
1. User registers in IdentityService.
2. IdentityService publishes `UserRegisteredEvent`.
3. AdminService creates pending staff.
4. Admin assigns store and role.
5. Admin publishes `StaffAssignedEvent`.
6. IdentityService creates the user-store-role mapping.

### Order + Payment Flow
1. OrdersService creates bill and items.
2. Bill is finalized to `PendingPayment`.
3. PaymentService collects payment and publishes `PaymentProcessedEvent`.
4. OrdersService finalizes bill and publishes `OrderPlacedEvent`.
5. CatalogService deducts stock.
6. AdminService syncs dashboard data.
7. NotificationService sends alerts.

### Return Flow
1. ReturnsService creates return and publishes `ReturnInitiatedEvent`.
2. OrdersService marks bill as returned.
3. On approval, ReturnsService publishes `OrderReturnedEvent`.
4. CatalogService restocks items.
5. AdminService syncs return data.
6. NotificationService sends updates.

## Error Handling and Reliability
1. Services handle exceptions with middleware.
2. Consumers are designed to retry on failure through MassTransit.
3. Events reduce coupling and allow partial system availability.

## Security Considerations
1. JWT tokens have limited lifetime and refresh tokens are rotated.
2. Permission-based authorization prevents unauthorized actions.
3. Multi-tenant isolation prevents cross-store data leakage.

## Future Improvements
1. Add centralized observability (metrics, tracing).
2. Add dead-letter queues for failed events.
3. Add API versioning and rate-limiting at the gateway.
4. Add automated tests for event flows.

## Conclusion
The RetailPOS system is a modular, event-driven microservices platform that supports secure retail operations at scale. It uses modern backend patterns such as API Gateway, JWT-based auth, permission enforcement, and asynchronous messaging to keep services independent while maintaining business consistency.
