namespace vaulterpAPI.Models.Inventory
{
    public class ItemDto
    {
        public int Id { get; set; }
        public int OfficeId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? MeasurementUnit { get; set; }
        public int MinStockLevel { get; set; }
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public string? BrandName { get; set; }
        public string? HSNCode { get; set; }
    }
}
