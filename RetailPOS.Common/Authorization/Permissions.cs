namespace RetailPOS.Common.Authorization
{
    public static class Permissions
    {
        public static class System
        {
            public const string All = "all:all";
            public const string UsersManage = "users:manage";
        }

        public static class Auth
        {
            public const string Refresh = "auth:token:refresh";
            public const string Logout = "auth:logout";
        }

        public static class Orders
        {
            public const string Create = "orders:create";
            public const string View = "orders:view";
            public const string ViewAll = "orders:view_all";
            public const string Finalize = "orders:finalize";
            public const string Hold = "orders:hold";
            public const string Void = "orders:void";
        }

        public static class Returns
        {
            public const string Initiate = "returns:initiate";
            public const string View = "returns:view";
            public const string Approve = "returns:approve";
        }

        public static class Payments
        {
            public const string CreateOrder = "payments:create_order";
            public const string Verify = "payments:verify";
        }

        public static class Admin
        {
            public const string ReportsView = "admin:reports:view";
            public const string InventoryAdjust = "admin:inventory:adjust";
            public const string StoresManage = "admin:stores:manage";
            public const string StoresView = "admin:stores:view";
        }

        public static class Catalog
        {
            public const string View = "catalog:view";
            public const string Manage = "catalog:manage";
            public const string Delete = "catalog:delete";
            public const string CategoriesEdit = "catalog:categories:edit";
            public const string CategoriesView = "catalog:categories:view";
        }

        public static class Notifications
        {
            public const string View = "notifications:view";
        }
    }
}
