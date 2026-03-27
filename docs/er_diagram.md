# Retail POS System - Database Entity Relationship Diagram

This diagram illustrates the data models and relationships across the microservices in the Retail POS system.

```mermaid
erDiagram
    %% Identity Service
    USER ||..|| STORE : "belongs to"
    USER {
        int id PK
        string email
        string passwordHash
        string role
        int primaryStoreId FK
    }
    STORE {
        int id PK
        string storeCode UK
        string name
        bool isActive
    }

    %% Catalog Service
    PRODUCT }o--|| CATEGORY : "categorized as"
    PRODUCT {
        int id PK
        string sku UK
        string barcode
        string name
        decimal sellingPrice
        int stockQuantity
        int categoryId FK
    }
    CATEGORY {
        int id PK
        string name
        string description
    }

    %% Orders Service
    BILL ||--o{ BILL_ITEM : "contains"
    BILL ||--o{ PAYMENT : "settled by"
    BILL }o--|| CUSTOMER : "purchased by"
    BILL {
        int id PK
        string billNumber UK
        datetime date
        int storeId
        int cashierId
        string customerMobile FK
        decimal totalAmount
        string status
    }
    BILL_ITEM {
        int id PK
        int billId FK
        int productId
        string productName
        decimal unitPrice
        int quantity
    }
    CUSTOMER {
        string mobile PK
        string name
        int storeId
    }
    PAYMENT {
        int id PK
        int billId FK
        string method
        decimal amount
    }

    %% Admin Service (Inventory)
    INVENTORY_ADJUSTMENT }o--|| PRODUCT : "adjusts"
    INVENTORY_ADJUSTMENT }o--|| STORE : "at"
    INVENTORY_ADJUSTMENT }o--|| USER : "by"
    INVENTORY_ADJUSTMENT {
        int id PK
        int storeId FK
        int productId FK
        int quantity
        string reasonCode
        int adjustedByUserId FK
    }
```

## Service Boundaries
*   **Identity Service**: Manages users, stores, and authentication.
*   **Catalog Service**: Manages the product catalog and categories.
*   **Orders Service**: Manages transactions, customers, and payments.
*   **Admin Service**: Manages inventory adjustments and dashboard analytics.
