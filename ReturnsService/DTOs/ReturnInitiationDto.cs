using System.Collections.Generic;

namespace ReturnsService.DTOs
{
    public class ReturnInitiationDto
    {
        public int BillId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string RefundMode { get; set; } = string.Empty;
        public string? CustomerMobile { get; set; }
        public List<ReturnItemDto> Items { get; set; } = new();
    }

    public class ReturnItemDto
    {
        public int BillItemId { get; set; }
        public int Quantity { get; set; }
        public decimal RefundAmount { get; set; }
        // We'll also need ProductId for the event, we can fetch it from the bill details in the service
    }
}
