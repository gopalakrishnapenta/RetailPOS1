# RETAIL POS SYSTEM
## Low-Level Design Document

**Document Status:** Draft  
**Document Type:** Low-Level Design (LLD)  
**System Name:** Retail POS System  
**Prepared By:** Reverse-engineered from implementation  
**Prepared On:** 12-Apr-2026  
**Intended Audience:** Developers, architects, technical reviewers

---

## 1. Document Control

### 1.1 Purpose
This document provides the Low-Level Design of the Retail POS implementation. It describes internal modules, responsibilities, data structures, API surfaces, runtime flows, validation behavior, logging model, and code-quality observations.

### 1.2 Scope
This document covers:

- service-level modules and responsibilities
- internal layering and ownership model
- inferred database schema by service
- API surface summary
- runtime sequence descriptions for billing, payment, inventory, and returns
- validation, error handling, and logging approach
- architecture and code-quality review findings

### 1.3 Relationship to HLD
This document should be read together with the HLD document. The HLD explains the system structure and architectural intent. The LLD explains how the current implementation realizes that design at the service and module level.

---

## 2. Internal Module Design

### 2.1 IdentityService

**Primary Purpose**  
Authentication, authorization support, and store-aware user identity management.

**Main Modules**

- `AuthController`
- `StoresController`
- `AuthService`
- `AppDbContext`
- `DbInitializer`
- store and staff synchronization consumers

**Detailed Responsibilities**

- authenticate users and issue JWT tokens
- handle OTP verification for login and account flows
- support email verification and password reset flows
- maintain role, permission, and store assignment data
- seed default roles and permission mappings

**Internal Structure**

- controllers manage transport and request handling
- `AuthService` contains authentication and token logic
- `AppDbContext` owns identity persistence
- `DbInitializer` seeds roles, permissions, and default admin data

### 2.2 CatalogService

**Primary Purpose**  
Product, category, and stock ownership.

**Main Modules**

- `ProductsController`
- `CategoriesController`
- `ProductService`
- `CategoryService`
- `CatalogDbContext`
- order, return, stock adjustment, and stock saga consumers

**Detailed Responsibilities**

- product CRUD and soft-delete lifecycle
- category CRUD and soft-delete lifecycle
- store-aware product retrieval
- stock deduction, stock addition, and stock restoration

**Internal Structure**

- controllers expose catalog endpoints
- `ProductService` contains stock and product business rules
- `CategoryService` owns category operations
- `CatalogDbContext` applies tenant-aware filters and store auto-assignment

### 2.3 OrdersService

**Primary Purpose**  
Billing and checkout ownership.

**Main Modules**

- `BillsController`
- `CustomersController`
- `BillService`
- `CustomerService`
- `OrdersDbContext`
- `CheckoutStateMachine`
- `SagaOrderCommandsConsumer`
- `PaymentProcessedConsumer`
- `ReturnInitiatedConsumer`
- `OrderReturnedConsumer`

**Detailed Responsibilities**

- create and update draft bills
- manage cart items and bill totals
- finalize, hold, and void bills
- maintain customer records
- coordinate distributed checkout progression
- update bill status after payment and return workflows

**Internal Structure**

- controllers expose bill and customer APIs
- `BillService` contains bill lifecycle logic
- `CheckoutStateMachine` controls distributed checkout transitions
- consumers respond to payment, return, and saga events

### 2.4 PaymentService

**Primary Purpose**  
Payment processing and payment record ownership.

**Main Modules**

- `PaymentsController`
- `PaymentService`
- `RazorpayService`
- `PaymentDbContext`
- `CheckoutSagaCommandsConsumer`

**Detailed Responsibilities**

- store payment records
- create online payment orders
- verify online payment signatures
- process payment and refund workflow commands
- publish payment completion events

**Internal Structure**

- controller exposes payment APIs
- `PaymentService` handles persistence and event publication
- `RazorpayService` isolates provider-specific logic
- consumer adapts saga commands into payment actions

### 2.5 ReturnsService

**Primary Purpose**  
Return request ownership and return workflow orchestration.

**Main Modules**

- `ReturnsController`
- `ReturnService`
- `ReturnsDbContext`
- `ReturnStateMachine`
- `SagaReturnCommandsConsumer`

**Detailed Responsibilities**

- create return requests
- persist item-level return entries
- approve or reject return requests
- coordinate refund and stock restock behavior
- finalize return lifecycle status

**Internal Structure**

- controller exposes return APIs
- `ReturnService` performs return business operations
- `ReturnStateMachine` controls approval, refund, restock, and completion
- consumer finalizes persisted return state

### 2.6 AdminService

**Primary Purpose**  
Administrative operations, reporting, and projection management.

**Main Modules**

- `DashboardController`
- `InventoryController`
- `ReportsController`
- `StaffController`
- `StoresController`
- `CategoriesController`
- `InventoryService`
- `DashboardService`
- `ReportService`
- `AdminDbContext`
- `DashboardEventsConsumer`
- `SagaUserOnboardingConsumer`

**Detailed Responsibilities**

- dashboard KPI retrieval
- inventory adjustment recording
- staff assignment
- store and category administration
- report generation
- synchronized reporting projections from order and return events

**Internal Structure**

- controllers expose admin-facing APIs
- `InventoryService` records stock adjustments and publishes events
- `DashboardService` calculates KPI data
- `ReportService` composes report data from local projections and OrdersService calls

### 2.7 NotificationService

**Primary Purpose**  
Notification persistence and real-time user delivery.

**Main Modules**

- `NotificationHub`
- `InternalNotificationService`
- notification consumers
- `NotificationDbContext`

**Detailed Responsibilities**

- create notifications from domain events
- persist notification records
- deliver notifications to browser clients over SignalR
- support email and SMS style channel expansion

**Internal Structure**

- consumers receive business events
- `InternalNotificationService` formats, stores, and dispatches notifications
- `NotificationHub` manages real-time client connectivity

---

## 3. Internal Responsibility Model

### 3.1 Controller Layer
The controller layer is responsible for:

- receiving HTTP requests
- validating route-level access through authorization attributes
- mapping transport payloads to service operations
- returning HTTP responses

### 3.2 Service Layer
The service layer is responsible for:

- executing business rules
- coordinating entity updates
- interacting with repository or DbContext abstractions
- publishing downstream events where required

### 3.3 Consumer Layer
The consumer layer is responsible for:

- processing asynchronous commands and events
- synchronizing local state with distributed workflows
- driving projections and downstream side effects

### 3.4 Saga Layer
The saga/state machine layer is responsible for:

- managing long-running workflow state
- handling success and compensation paths
- reducing direct synchronous dependency between services

---

## 4. Persistence Design

### 4.1 IdentityService Schema

| Entity | Key | Important Fields | Notes |
|---|---|---|---|
| `Users` | `Id` | `Email`, `PasswordHash`, `EmployeeCode`, OTP fields, refresh token fields | user identity and authentication data |
| `Stores` | `Id` | `StoreCode`, `Name`, `Location`, `IsActive` | active store master data |
| `Roles` | `Id` | `Name` | role master |
| `Permissions` | `Id` | `Code`, `Description` | permission master |
| `UserStoreRoles` | `Id` | `UserId`, `StoreId`, `RoleId` | store-aware role assignment |
| `RolePermissions` | composite | `RoleId`, `PermissionId` | role-permission mapping |

**Relationship Summary**

- one user can have multiple role assignments across stores
- one role can contain multiple permissions
- one store can contain multiple user-role mappings

### 4.2 CatalogService Schema

| Entity | Key | Important Fields | Notes |
|---|---|---|---|
| `Categories` | `Id` | `Name`, `Description`, `IsActive`, `StoreId` | product grouping |
| `Products` | `Id` | `Sku`, `Barcode`, `Name`, `MRP`, `SellingPrice`, `TaxCode`, `ReorderLevel`, `StockQuantity`, `CategoryId`, `StoreId`, `IsActive` | store-specific sellable item |

**Relationship Summary**

- one category is associated with many products
- product records are intended to be store-isolated

### 4.3 OrdersService Schema

| Entity | Key | Important Fields | Notes |
|---|---|---|---|
| `Bills` | `Id` | `BillNumber`, `Date`, `StoreId`, `CashierId`, `CustomerMobile`, `CustomerName`, `TotalAmount`, `TaxAmount`, `Status` | billing aggregate root |
| `BillItems` | `Id` | `BillId`, `ProductId`, `ProductName`, `UnitPrice`, `Quantity`, `SubTotal`, `StoreId` | bill line items |
| `Customers` | `Id` | `Mobile`, `Name`, `CreatedAt`, `StoreId` | store-specific customer data |

**Relationship Summary**

- one bill contains multiple bill items
- customer association is business-driven rather than strict foreign-key driven

### 4.4 PaymentService Schema

| Entity | Key | Important Fields | Notes |
|---|---|---|---|
| `Payments` | `Id` | `BillId`, `PaymentMode`, `Amount`, `ReferenceNumber`, `StoreId` | payment transaction record |

### 4.5 ReturnsService Schema

| Entity | Key | Important Fields | Notes |
|---|---|---|---|
| `Returns` | `Id` | `OriginalBillId`, `ProductId`, `Quantity`, `RefundAmount`, `Reason`, `Status`, `ManagerApprovalNote`, `CustomerMobile`, `StoreId`, `Date` | item-level return record |

### 4.6 AdminService Schema

| Entity | Key | Important Fields | Notes |
|---|---|---|---|
| `InventoryAdjustments` | `Id` | `StoreId`, `ProductId`, `Quantity`, `ReasonCode`, `DocumentReference`, `AdjustmentDate`, `AdjustedByUserId`, `IsApproved` | inventory adjustment ledger |
| `Stores` | `Id` | `StoreCode`, `Name`, `Location`, `IsActive` | admin store master |
| `Categories` | `Id` | `Name`, `Description`, `IsActive`, `StoreId` | admin category master |
| `SyncedOrders` | `OrderId` | `StoreId`, `TotalAmount`, `TaxAmount`, `Date`, `CustomerMobile` | reporting projection |
| `SyncedReturns` | `Id` | `OrderId`, `ReturnId`, `RefundAmount`, `StoreId`, `Date` | return reporting projection |
| `DashboardStats` | `Id` | `TotalSales`, `TodaySales`, `TotalBills`, `TodayBills`, `LowStockAlerts`, `LastUpdated` | aggregated KPI snapshot |
| `StaffMembers` | `UserId` | `Email`, `FullName`, `IsAssigned`, `AssignedStoreId`, `AssignedRole`, `RegisteredDate` | staff projection |

### 4.7 NotificationService Schema
The notification service persists notification records before or alongside delivery. The implementation clearly indicates owned persistence for notification metadata, although the schema is less explicit than the core transactional services.

---

## 5. API Surface Summary

### 5.1 Identity APIs

| Endpoint | Method | Purpose |
|---|---|---|
| `/gateway/auth/login` | `POST` | authenticate user |
| `/gateway/auth/verify-login-otp` | `POST` | verify login OTP |
| `/gateway/auth/register` | `POST` | register user/staff |
| `/gateway/auth/verify-email` | `POST` | verify email |
| `/gateway/auth/resend-verification` | `POST` | resend verification OTP |
| `/gateway/auth/send-otp` | `POST` | issue OTP for recovery or login flow |
| `/gateway/auth/reset-password` | `POST` | reset password |
| `/gateway/auth/google-login` | `POST` | external login |
| `/gateway/auth/refresh` | `POST` | refresh token |
| `/gateway/auth/logout` | `POST` | logout |
| `/gateway/stores/active` | `GET` | list active stores |

### 5.2 Catalog APIs

| Endpoint | Method | Purpose |
|---|---|---|
| `/gateway/products` | `GET` | list store products |
| `/gateway/products/all` | `GET` | list broader product set |
| `/gateway/products` | `POST` | create product |
| `/gateway/products/{id}` | `PUT` | update product |
| `/gateway/products/{id}` | `DELETE` | soft delete product |
| `/gateway/products/{id}/restore` | `POST` | restore product |
| `/gateway/categories` | `GET` | list categories |
| `/gateway/categories/all` | `GET` | list all categories |
| `/gateway/categories/{id}` | `GET` | get category detail |
| `/gateway/categories` | `POST` | create category |
| `/gateway/categories/{id}` | `PUT` | update category |
| `/gateway/categories/{id}` | `DELETE` | soft delete category |
| `/gateway/categories/{id}/restore` | `POST` | restore category |

### 5.3 Orders APIs

| Endpoint | Method | Purpose |
|---|---|---|
| `/gateway/bills` | `GET` | list bills |
| `/gateway/bills/{id}` | `GET` | get bill detail |
| `/gateway/bills` | `POST` | create or update draft bill |
| `/gateway/bills/cart/items` | `POST` | update cart items |
| `/gateway/bills/{id}/finalize` | `POST` | start checkout flow |
| `/gateway/bills/{id}/hold` | `POST` | hold bill |
| `/gateway/bills/{id}/void` | `POST` | void bill |
| `/gateway/customers` | `GET` | list customers |
| `/gateway/customers/{mobile}` | `GET` | get customer by mobile |
| `/gateway/customers` | `POST` | create or update customer |

### 5.4 Payment APIs

| Endpoint | Method | Purpose |
|---|---|---|
| `/gateway/payments/collect` | `POST` | store payment and publish success event |
| `/gateway/payments/create-order` | `POST` | create Razorpay order |
| `/gateway/payments/verify` | `POST` | verify Razorpay signature |

### 5.5 Return APIs

| Endpoint | Method | Purpose |
|---|---|---|
| `/gateway/returns` | `GET` | list returns |
| `/gateway/returns` | `POST` | initiate return |
| `/gateway/returns/{id}/approve` | `POST` | approve return |
| `/gateway/returns/{id}/reject` | `POST` | reject return |

### 5.6 Admin APIs

| Endpoint | Method | Purpose |
|---|---|---|
| `/gateway/dashboard/stats` | `GET` | get dashboard metrics |
| `/gateway/inventory/adjust` | `POST` | create inventory adjustment |
| `/gateway/inventory` | `GET` | get inventory summary/history |
| `/gateway/reports/sales` | `GET` | get sales report |
| `/gateway/reports/tax` | `GET` | get tax report |
| `/gateway/staff` | `GET` | list staff |
| `/gateway/staff/pending` | `GET` | list unassigned staff |
| `/gateway/staff/assign` | `POST` | assign store and role |
| `/gateway/stores` | `GET` | list stores |
| `/gateway/stores/all` | `GET` | list all stores |
| `/gateway/stores` | `POST` | create store |
| `/gateway/stores/{id}` | `PUT` | update store |
| `/gateway/stores/{id}` | `DELETE` | delete store |
| `/gateway/stores/{id}/restore` | `POST` | restore store |

---

## 6. Runtime Processing Design

### 6.1 Billing Flow

```text
Step 1  Cashier selects products in POS UI.
Step 2  UI sends bill/cart payload to OrdersService.
Step 3  OrdersService stores the bill as Draft.
Step 4  Cashier finalizes the bill.
Step 5  OrdersService changes bill state to PendingPayment and emits checkout initiation.
Step 6  Payment processing is triggered.
Step 7  Successful payment triggers stock deduction.
Step 8  Successful stock deduction finalizes the order.
Step 9  Admin projections and notifications are updated from order events.
```

### 6.2 Payment Processing Flow

```text
Cash Payment Path
-----------------
Step 1  UI calls payment collection API.
Step 2  PaymentService stores the payment.
Step 3  PaymentService publishes payment success event.
Step 4  OrdersService advances bill workflow.

Online Payment Path
-------------------
Step 1  UI requests Razorpay order creation.
Step 2  PaymentService creates provider order.
Step 3  User completes payment through Razorpay.
Step 4  UI sends verification request.
Step 5  PaymentService verifies provider signature.
Step 6  UI calls payment collection API.
Step 7  PaymentService persists the payment and publishes success event.
```

### 6.3 Inventory Update Flow

```text
Step 1  Admin user submits stock adjustment.
Step 2  AdminService records the adjustment.
Step 3  AdminService publishes stock adjustment event.
Step 4  CatalogService consumes the event.
Step 5  Product stock is updated.
Step 6  NotificationService may generate operational alerts.
```

### 6.4 Return Processing Flow

```text
Step 1  User selects a prior bill in the returns screen.
Step 2  Return items and quantities are submitted to ReturnsService.
Step 3  ReturnsService stores return rows and emits return initiation event.
Step 4  Manager approves or rejects the return.
Step 5  If approved, refund command is issued.
Step 6  After refund confirmation, restock command is issued.
Step 7  Return is finalized and synchronized to related services.
```

---

## 7. Validation and Error Handling

### 7.1 Validation Controls
Validation is implemented through:

- JWT-based authentication
- policy-based authorization with permissions
- store-aware context resolution from claims and headers
- service-layer business validation
- EF Core tenant query filtering

### 7.2 Error Handling Controls
Error handling is implemented through:

- centralized exception middleware
- standardized API problem responses
- trace identifier inclusion
- HTTP status mapping for domain and validation failures
- compensation flow in distributed checkout scenarios

### 7.3 Observed Gaps
The following validation and correctness gaps are present:

- return refund contracts are not fully aligned across services
- some APIs still use entity models directly instead of transport DTOs
- some reporting and inventory behavior still contains placeholder or shortcut logic

---

## 8. Logging and Monitoring

### 8.1 Current Logging Design
The current implementation uses Serilog for:

- console logging
- rolling file logging
- service name enrichment
- exception capture

### 8.2 Recommended Monitoring Enhancements
Recommended additions for production maturity:

- health and readiness endpoints per service
- distributed trace correlation across gateway, APIs, and message bus
- retry, failure, and dead-letter monitoring for asynchronous workflows
- operational dashboards for checkout failure, refund failure, and stock deduction failure

---

## 9. Architecture and Code Quality Review

### 9.1 SOLID Assessment

**Single Responsibility Principle**  
Mostly followed at controller and service level. Reduced in quality where saga, consumer, and service responsibilities overlap for the same business outcome.

**Open/Closed Principle**  
Reasonably supported through shared contracts, consumers, and dynamic authorization policies.

**Liskov Substitution Principle**  
Not a major concern in the current implementation because inheritance is limited.

**Interface Segregation Principle**  
Generally acceptable. Service abstractions and repositories are reasonably focused.

**Dependency Inversion Principle**  
Largely followed through dependency injection, abstractions, and shared infrastructure registration.

### 9.2 Design Patterns Identified
The implementation explicitly or implicitly uses:

- API Gateway pattern
- Service Layer pattern
- Repository pattern
- Saga pattern
- Publish/Subscribe pattern
- Middleware pattern
- Policy-based authorization pattern
- Observer-style notification pattern using SignalR

### 9.3 Strengths
- good domain separation
- strong multi-store intent
- meaningful use of asynchronous workflow orchestration
- reporting via projections rather than cross-service schema dependency
- consistent use of shared libraries for cross-cutting concerns

### 9.4 Issues Requiring Attention

**1. Duplicate order completion publication**  
`OrderPlacedEvent` is emitted from more than one point in the checkout path. This can cause duplicate stock updates, duplicate reporting, and duplicate notifications.

**2. Refund command mismatch**  
Return workflow code publishes refund commands that do not fully align with the shared contract definition.

**3. Placeholder refund amount logic**  
Refund amount calculation in the return saga uses placeholder logic rather than persisted financial data.

**4. Hard-coded store identifier in inventory adjustment**  
This introduces a real tenant isolation defect and conflicts with the intended architecture.

**5. SignalR contract drift**  
The frontend invokes `JoinStoreGroup`, but the hub implementation does not expose that method.

**6. DTO boundary inconsistency**  
Some endpoints still accept or expose persistence entities directly.

**7. Mixed workflow ownership**  
Order completion status and related events are controlled from multiple layers, reducing clarity and idempotency.

---

## 10. Recommended Engineering Improvements

### 10.1 Immediate Actions
- make checkout finalization single-owner and idempotent
- align refund contracts and derive refund amount from persisted values
- remove hard-coded store handling from inventory adjustment
- align SignalR client and backend contracts

### 10.2 Short-Term Improvements
- standardize DTO-only controller contracts
- introduce explicit message correlation and idempotency controls
- improve workflow-focused monitoring and integration testing

### 10.3 Medium-Term Improvements
- define projection rebuild strategy for reporting
- improve admin-side product master synchronization
- strengthen automated end-to-end testing for billing and returns

---

## 11. Conclusion
The current implementation reflects a solid low-level design foundation for a production-style Retail POS platform. Service boundaries, workflow orchestration, and shared platform conventions are well established. The primary technical work ahead is refinement: eliminating conflicting workflow ownership, enforcing full tenant correctness, and tightening API and contract consistency.
