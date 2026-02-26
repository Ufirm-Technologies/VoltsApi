namespace vaulterpAPI.Models.OfficeMaster
{
    public class OfficeDto
    {
        public int OfficeId { get; set; }
        public string OfficeName { get; set; }
        public string OfficeType { get; set; }
        public string Region { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string? Pincode { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public bool IsActive { get; set; } = true;
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
    }

}
