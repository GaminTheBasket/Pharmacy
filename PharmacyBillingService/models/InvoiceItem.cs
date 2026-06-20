using System.Text.Json.Serialization;

namespace PharmacyBillingService.Models
{
    public class InvoiceItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        [JsonIgnore]
        public Invoice? Invoice { get; set; }
        public Medicine? Medicine { get; set; }
    }
}
