export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

export interface User {
  id: number;
  email: string;
  fullName: string;
  role: string;
  storeId: number;
  storeCode?: string;
  permissions: string[];
}

export interface Store {
  id: number;
  name: string;
  storeCode: string;
  location?: string;
  isActive: boolean;
}

export interface Product {
  id: number;
  name: string;
  sku: string;
  categoryId: number;
  categoryName?: string;
  sellingPrice: number;
  costPrice: number;
  stockQuantity: number;
  imagePath?: string;
}

export interface Category {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface BillItem {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  subTotal: number;
}

export interface Bill {
  id?: number;
  billNumber: string;
  customerName?: string;
  customerMobile: string;
  totalAmount: number;
  taxAmount: number;
  storeId: number;
  cashierId: number;
  status: 'Draft' | 'Held' | 'PendingPayment' | 'Paid' | 'Cancelled';
  items: BillItem[];
  date?: string;
}

export interface PaymentRequest {
  billId: number;
  amount: number;
  paymentMode: 'Cash' | 'Online' | 'Card';
  transactionId?: string;
}

export interface DashboardStats {
  totalSales: number;
  totalOrders: number;
  totalCustomers: number;
  lowStockItems: number;
  recentOrders: Bill[];
}
