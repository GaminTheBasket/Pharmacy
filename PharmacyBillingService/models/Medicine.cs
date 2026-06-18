namespace PharmacyBillingService.Models
{
    public class Medicine
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ActiveIngredient { get; set; } = string.Empty; // Hoạt chất
        public string Unit { get; set; } = string.Empty; // Đơn vị tính (viên, vỉ, hộp)
        public decimal Price { get; set; }
        public int StockQuantity { get; set; } // Số lượng tồn kho
    }
}
