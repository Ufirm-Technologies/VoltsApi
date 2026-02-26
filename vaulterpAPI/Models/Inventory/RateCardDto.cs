namespace vaulterpAPI.Models
{
    public class RateCardDto
    {
        public int Id { get; set; }

        // Basic fields
        public int? ItemId { get; set; }
        public int? VendorId { get; set; }
        public int? CategoryId { get; set; }
        public decimal Price { get; set; }
        public DateTime? ValidTill { get; set; }
        public int CreatedBy { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedOn { get; set; }

        // Additional fields for retrieval
        public string? CategoryName { get; set; }
        public string? ItemName { get; set; }
        public string? BrandName { get; set; }
        public string? Description { get; set; }
        public string? HSNCode { get; set; }
        public string? VendorName { get; set; }
        public string? MeasurementUnit { get; set; }
    }
}
