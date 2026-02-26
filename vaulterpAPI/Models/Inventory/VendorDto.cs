namespace vaulterpAPI.Models
{
    public class VendorDto
    {
        public int Id { get; set; }
        public int OfficeId { get; set; }
        public string Name { get; set; } = string.Empty;

        public string? ContactPerson { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? GSTNumber { get; set; }
        public string? PANNumber { get; set; }

        public bool IsApproved { get; set; }
        public int? ApprovedBy { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; }

        // Optional fields for files (used in multi-part requests)
        public string? PANFileUrl { get; set; }
        public string? GSTCertificateUrl { get; set; }
        public string? BrochureUrl { get; set; }
        public string? WebsiteUrl { get; set; }
    }
}
