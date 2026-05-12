# Software Requirements Specification (SRS)
## Project: RetailPOS - Advanced Microservices Point of Sale

### 1. Introduction
#### 1.1 Purpose
The purpose of this document is to provide a detailed overview of the RetailPOS system, its architecture, functional requirements, and technical specifications. This system is designed to provide a scalable, secure, and intelligent platform for retail businesses to manage sales, inventory, and staff.

#### 1.2 Scope
RetailPOS is a distributed system built on a microservices architecture. It includes a modern web interface for store staff and managers, an intelligent AI assistant for inventory insights, and a robust backend capable of handling high transaction volumes through asynchronous processing.

---

### 2. System Architecture
The system follows a **Microservices Architecture** using **ASP.NET Core** and **Angular**.

*   **API Gateway (Ocelot)**: Centralized entry point for all client requests, handling routing, authentication validation, and CORS policies.
*   **Identity Service**: Manages User Identity, Role-Based Access Control (RBAC), and Google OAuth integration.
*   **Business Services**: Independent services for Catalog, Orders, Admin, Returns, and Payments.
*   **AI Service**: Integration with Google Gemini for natural language queries about store data.
*   **Message Broker (RabbitMQ)**: Asynchronous communication between services using the Publish/Subscribe pattern via MassTransit.
*   **Data Layer**: SQL Server for persistence, Redis for caching, and Docker Volumes for data durability.

---

### 3. Functional Requirements

#### 3.1 Authentication & Security
*   **Multi-Factor Auth**: Secure login via email-based OTP verification.
*   **Social Login**: Integration with Google Identity Services.
*   **RBAC**: Fine-grained permissions for Admin, StoreManager, and Cashier roles.
*   **Session Management**: JWT-based stateless authentication with secure token refresh logic.

#### 3.2 Inventory & Catalog Management
*   **Product Management**: Create, update, and track products across multiple stores.
*   **Low Stock Alerts**: Real-time monitoring of inventory levels with automated alerts.
*   **Multi-Store Support**: Ability to manage stock levels independently per store location.

#### 3.3 Sales & Billing (POS)
*   **Billing Interface**: High-performance UI for fast checkout.
*   **Transaction Ledger**: Real-time recording of sales with server-side pagination for millions of records.
*   **Returns Management**: Workflow for initiating and approving product returns with automatic inventory restock.

#### 3.4 AI Assistant (Store Insights)
*   **Contextual Queries**: Ask questions like "Which products are out of stock?" or "What is the highest priced item?"
*   **Live Data Sync**: AI uses real-time inventory and category data to provide accurate answers.

#### 3.5 Admin Dashboard
*   **Real-time Stats**: Live visualization of Today's Sales, Transactions, and Refunds.
*   **Staff Management**: View and manage employee performance and assignments.

---

### 4. Non-Functional Requirements
*   **Scalability**: Services can be scaled independently using Docker/Kubernetes.
*   **Durability**: Database persistence ensured via Docker Named Volumes.
*   **Performance**: Gateway timeouts configured for slow operations (e.g., 30s for Email/OTP).
*   **Security**: Implementation of COOP/CORS policies and secure JWT validation.

---

### 5. Technical Stack
*   **Frontend**: Angular 18+ (Standalone Components, Signals, Tailwind CSS).
*   **Backend**: .NET 9.0 / .NET 10.0 (C#, Web API).
*   **Gateway**: Ocelot API Gateway.
*   **Database**: Microsoft SQL Server.
*   **Messaging**: RabbitMQ + MassTransit.
*   **AI**: Google Gemini API (v1beta/v1).
*   **Containerization**: Docker & Docker Compose.
*   **Logging/Monitoring**: Serilog + Seq.

---

### 6. System Data Flow
1.  **Client Request**: Angular UI ➔ API Gateway.
2.  **Auth Check**: Gateway validates JWT ➔ Forwards to internal service.
3.  **Operation**: Service processes request ➔ Updates SQL Server.
4.  **Notification**: Service publishes event to RabbitMQ ➔ Other services (e.g., Notification) consume and react.
5.  **Response**: Results returned via Gateway to UI.
