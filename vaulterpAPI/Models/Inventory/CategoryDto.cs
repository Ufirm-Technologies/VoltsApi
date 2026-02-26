namespace vaulterpAPI.Models.Inventory
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public int OfficeId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public int? ApprovedBy { get; set; }
    }
}

