namespace PharmacyBillingService.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string PatientName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public decimal ConsultationFee { get; set; }
        public decimal MedicineFee { get; set; }
        public decimal TotalAmount { get; set; }
        public List<InvoiceItem> Items { get; set; } = new();
    }
}
