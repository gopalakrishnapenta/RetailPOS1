using System.ComponentModel.DataAnnotations;

namespace AdminService.Models
{
    public class SyncedProduct
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Sku { get; set; }
        public int StoreId { get; set; }
    }
}
