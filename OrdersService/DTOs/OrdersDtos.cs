using System.ComponentModel.DataAnnotations;

namespace OrdersService.DTOs
{
    public class BillDto
    {
        public int Id { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Customer mobile must be exactly 10 digits")]
        public string? CustomerMobile { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int StoreId { get; set; }
        public int CashierId { get; set; }
        public List<BillItemDto> Items { get; set; } = new();
    }

    public class BillItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class CustomerDto
    {
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile must be exactly 10 digits")]
        public string? Mobile { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ReturnDetailedDto
    {
        public int Id { get; set; }
        public int OriginalBillId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
