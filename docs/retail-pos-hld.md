# RETAIL POS SYSTEM
## High-Level Design Document

**Document Status:** Draft  
**Document Type:** High-Level Design (HLD)  
**System Name:** Retail POS System  
**Prepared By:** Reverse-engineered from implementation  
**Prepared On:** 12-Apr-2026  
**Intended Audience:** Architects, senior developers, technical reviewers, interview panelists

---

## 1. Document Control

### 1.1 Purpose
This document provides the High-Level Design of the Retail POS system based on the implemented codebase. It focuses on business context, architectural structure, system boundaries, major components, integration patterns, deployment considerations, and high-level design decisions.

### 1.2 Scope
This document covers:

- overall system purpose and architecture style
- logical decomposition into major services and UI components
- inter-service communication model
- external integrations
- multi-store design approach
- deployment, scalability, and availability considerations
- high-level architectural strengths, trade-offs, and risks

This document does not go into class-level design, detailed schemas, or endpoint-level API description. Those topics are covered in the LLD document.

### 1.3 Intended Use
This document is suitable for:

- architecture review meetings
- portfolio presentation
- senior developer interviews
- solution onboarding discussions

### 1.4 Reference Implementation
The document is derived from the current codebase under:

- `ApiGateway`
- `IdentityService`
- `CatalogService`
- `OrdersService`
- `PaymentService`
- `ReturnsService`
- `AdminService`
- `NotificationService`
- `pos-ui`
- `RetailPOS.Common`
- `RetailPOS.Contracts`

---

## 2. Executive Summary
The Retail POS system is a multi-store retail platform designed to support day-to-day store operations and centralized administration. The implemented solution uses a microservices-based backend with an Angular front end and a gateway-based entry layer. The system supports billing, payments, returns, inventory operations, user and staff management, reporting, and real-time notifications.

From a business standpoint, the platform enables separate store operations while maintaining centralized administrative control. From a technical standpoint, it combines synchronous REST APIs for user-driven actions with asynchronous event-based workflows for checkout, stock updates, returns, reporting, and notifications.

The architecture demonstrates a strong production-oriented direction. The most significant design risks are concentrated in workflow consistency and a few implementation-level contract mismatches, rather than in the overall architectural model.

---

## 3. Business Context

### 3.1 Business Objective
The system is intended to support retail store operations across multiple stores, while preserving store-level data boundaries and allowing selected administrative users to work across stores.

### 3.2 Business Capabilities
The platform supports the following primary business capabilities:

- billing and point-of-sale operations
- customer lookup and customer history support
- product and category management
- online and offline payment handling
- return initiation and approval workflows
- stock adjustments and inventory maintenance
- store, staff, and role administration
- operational reporting and dashboard metrics
- real-time event notification for store users

### 3.3 Primary Users
The principal user categories are:

- cashier
- store manager
- administrator

---

## 4. System Overview

### 4.1 Solution Summary
The solution is composed of:

- one Angular-based client application
- one API Gateway
- multiple independently deployed backend services
- service-owned SQL persistence
- asynchronous integration through shared contracts
- real-time delivery through SignalR

### 4.2 Architectural Style
The implemented architecture is best classified as:

- microservices architecture at the solution level
- layered architecture inside each service
- event-driven orchestration for distributed business transactions

### 4.3 Architectural Characteristics
Key architectural characteristics include:

- clear domain separation by service
- service-owned data models
- gateway-mediated client access
- token-based authorization with store context propagation
- asynchronous workflow handling using sagas/state machines
- projection-driven reporting instead of shared database access

---

## 5. Major System Components

### 5.1 Client Layer
**Component:** `pos-ui`  
**Technology:** Angular

**Responsibilities**

- user login and session handling
- POS billing interface
- payment interface
- returns interface
- admin dashboard and maintenance screens
- SignalR-based real-time notification consumption

### 5.2 Gateway Layer
**Component:** `ApiGateway`  
**Technology:** Ocelot

**Responsibilities**

- expose a single API surface to the UI
- route requests to downstream services
- centralize ingress behavior
- pass authorization context to backend services

### 5.3 Identity Domain
**Component:** `IdentityService`

**Responsibilities**

- user registration and authentication
- OTP verification
- email verification
- store management lookup
- user-role-store mapping
- permission-based authorization enablement

### 5.4 Catalog Domain
**Component:** `CatalogService`

**Responsibilities**

- product master management
- category management
- stock quantity maintenance
- stock update processing from events

### 5.5 Orders Domain
**Component:** `OrdersService`

**Responsibilities**

- bill creation and draft management
- cart item management
- bill hold and void actions
- bill finalization
- checkout workflow participation

### 5.6 Payment Domain
**Component:** `PaymentService`

**Responsibilities**

- payment record creation
- online payment integration with Razorpay
- payment verification
- payment event publication

### 5.7 Returns Domain
**Component:** `ReturnsService`

**Responsibilities**

- return request creation
- return approval and rejection
- refund and restock workflow coordination

### 5.8 Administration Domain
**Component:** `AdminService`

**Responsibilities**

- dashboard metrics
- inventory adjustment
- staff assignment
- store administration
- reporting projections

### 5.9 Notification Domain
**Component:** `NotificationService`

**Responsibilities**

- event-driven notification creation
- SignalR-based notification delivery
- notification persistence
- email/SMS style delivery hooks

### 5.10 Shared Platform Components
**Components**

- `RetailPOS.Common`
- `RetailPOS.Contracts`

**Responsibilities**

- shared permissions and authorization model
- logging conventions
- exception handling conventions
- command and event contracts

---

## 6. Logical Architecture View

### 6.1 Component Interaction Summary
The system operates through the following interaction model:

```text
User
  |
  v
Angular POS/Admin Application
  |
  v
API Gateway
  |
  +--> IdentityService
  +--> CatalogService
  +--> OrdersService
  +--> PaymentService
  +--> ReturnsService
  +--> AdminService
  +--> NotificationService

Asynchronous business communication:
- OrdersService communicates with PaymentService and CatalogService through contracts and message-driven workflows
- ReturnsService communicates with PaymentService and CatalogService through contracts and message-driven workflows
- AdminService receives order and return events for reporting projections
- NotificationService receives business events for user/store notifications
```

### 6.2 Data Ownership Principle
Each service owns its own data model and persistence boundary. Cross-service reporting is achieved through event synchronization and projections rather than shared database joins.

---

## 7. High-Level Functional Flow

### 7.1 Authentication Flow
1. The user logs in from the Angular application.
2. The request is routed through API Gateway to IdentityService.
3. IdentityService validates credentials and OTP where applicable.
4. IdentityService returns a JWT containing role, permission, and store context.
5. The client uses the token for subsequent requests.

### 7.2 Billing and Checkout Flow
1. The cashier creates a bill from the POS screen.
2. OrdersService stores the bill as a draft.
3. The cashier finalizes the bill.
4. OrdersService initiates the checkout workflow.
5. Payment processing and stock deduction are coordinated asynchronously.
6. Successful completion results in downstream updates to reporting and notifications.

### 7.3 Return Flow
1. The user selects a previous bill in the returns interface.
2. The return request is created in ReturnsService.
3. A manager approves or rejects the request.
4. Approved requests trigger refund and restocking flow.
5. Completion is synchronized to the related order, reporting, and notification services.

### 7.4 Inventory Adjustment Flow
1. An admin user creates an inventory adjustment.
2. AdminService records the adjustment.
3. A stock adjustment event is published.
4. CatalogService updates product stock.
5. NotificationService may publish operational alerts.

---

## 8. External Integrations

### 8.1 Payment Integration
**Provider:** Razorpay

**Usage**

- payment order creation
- payment signature verification

### 8.2 Real-Time Communication
**Technology:** SignalR

**Usage**

- push notifications to browser clients
- store-aware user notification delivery

### 8.3 Data Storage
**Technology:** SQL Server

**Usage**

- service-owned transactional persistence
- projection and reporting storage

### 8.4 Messaging Infrastructure
**Technology:** MassTransit-based contract integration over a message bus

**Usage**

- distributed workflow coordination
- event propagation
- asynchronous service decoupling

---

## 9. Multi-Store Architecture

### 9.1 Design Intent
The platform is designed as a multi-store system in which each store operates against its own data domain while selected admin users may access cross-store views.

### 9.2 Store Context Propagation
Store identity is propagated through:

- JWT claims
- `X-Store-Id` override header for authorized admin scenarios
- tenant-aware query filtering in service persistence layers

### 9.3 Expected Isolation Behavior
The intended business behavior is:

- each store should see its own products
- each store should see its own bills and customers
- each store should see its own returns and inventory data
- admin users may access broader views only where explicitly allowed

---

## 10. Deployment Considerations

### 10.1 Deployment Model
A practical deployment topology is:

```text
Client Browser
  |
  v
Web Server / Reverse Proxy
  |
  +--> Angular UI
  +--> API Gateway
           |
           +--> IdentityService
           +--> CatalogService
           +--> OrdersService
           +--> PaymentService
           +--> ReturnsService
           +--> AdminService
           +--> NotificationService

Shared supporting infrastructure:
- SQL Server
- Message Broker
- Razorpay integration
```

### 10.2 Scalability Considerations
- stateless APIs can be horizontally scaled
- asynchronous workflows reduce synchronous dependencies
- reporting can scale through projections rather than live joins
- services can be tuned independently according to load type

### 10.3 Availability Considerations
- the gateway is a critical entry component
- the message bus is critical for checkout and return completion
- reporting and notifications are eventually consistent by design
- SignalR scale-out requires multi-instance coordination support

---

## 11. Design Strengths
The implementation demonstrates the following high-level strengths:

- clear domain-driven separation between identity, catalog, orders, payments, returns, administration, and notifications
- strong use of contracts for asynchronous integration
- multi-store behavior is designed into authentication and data access layers
- reporting is projection-based rather than dependent on shared schemas
- real-time notification support is integrated into the architecture

---

## 12. Design Trade-offs
The architecture reflects the following trade-offs:

- microservices provide modularity and extensibility but increase operational complexity
- asynchronous orchestration improves decoupling but introduces eventual consistency
- service-owned persistence improves autonomy but requires explicit synchronization patterns
- a single gateway simplifies the client model but centralizes ingress dependency

---

## 13. Architectural Risks
The high-level architecture is sound, but several implementation-level issues reduce design consistency:

- duplicate order completion publication in the checkout flow
- inconsistent refund contract usage in return processing
- incomplete tenant enforcement in selected admin paths
- drift between some frontend and backend integration contracts

These issues do not invalidate the architecture, but they should be corrected to align runtime behavior with the intended design.

---

## 14. Conclusion
The Retail POS system represents a strong high-level architecture for a multi-store retail platform. It goes beyond a simple CRUD implementation and demonstrates meaningful architectural capability through service decomposition, distributed workflows, authorization boundaries, reporting projections, and real-time operational notification.

With targeted refinement in checkout consistency, refund correctness, and tenant enforcement, this system is suitable for formal architecture discussion and senior-level technical presentation.
