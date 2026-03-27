# Retail POS System - Technology Stack & Core Concepts

This document summarizes the technologies and architectural concepts used in the building of your Retail POS Billing & Store Management System.

## Technology Stack

### Backend: ASP.NET Core Microservices
*   **IdentityService**: Handles User Authentication, RBAC (Role-Based Access Control), and Store Master data. Uses JWT (JSON Web Tokens) for security.
*   **CatalogService**: Manages the Product database, Categories, and SKU generation logic.
*   **OrdersService**: Handles the core transaction logic (Bills, Payments, Inventory sync, Customers).
*   **AdminService**: Manages Dashboards, Reports, and Inventory Adjustments.
*   **Ocelot API Gateway**: Acts as a single entry point for the frontend, routing requests to the correct microservice.

### Frontend: Angular (v17+)
*   **TypeScript**: Provides type-safe development.
*   **RxJS**: Handles asynchronous data streams and API calls.
*   **Standalone Components**: Modern Angular architecture for better performance and modularity.
*   **Vanilla CSS**: Custom premium UI design system with responsive layouts.

### Data & Persistence
*   **Entity Framework Core (EF Core)**: Object-Relational Mapper (ORM) for database interactions.
*   **SQLite / SQL Server**: Relational databases for persistent storage.
*   **EF Migrations**: Used for version-controlled database schema updates and data seeding.

---

## Core Architectural Concepts

### 1. Microservices Architecture
The system is divided into small, independent services. This allows for:
*   **Scalability**: Services can be scaled independently.
*   **Resilience**: If one service fails, others can often stay operational.
*   **Maintainability**: Codebases are smaller and easier to manage.

### 2. API Gateway Pattern
The **Ocelot** gateway provides a unified interface (`http://localhost:5000/gateway/...`) for the frontend. It handles:
*   **Routing**: Mapping frontend URLs to backend microservices.
*   **CORS**: Managing cross-origin resource sharing.

### 3. Role-Based Access Control (RBAC)
*   Access is restricted based on the user's role: **Admin**, **Manager**, or **Cashier**.
*   Implemented via JWT claims on the backend and route guards on the frontend.

### 4. Reactive State Management
*   The frontend uses RxJS observables to ensure real-time updates (e.g., live search, stock status changes).

### 5. Automated SKU Prefixing
*   A reactive logic in the `ProductsComponent` that calculates a product's SKU prefix based on the selected category whenever the form changes.
