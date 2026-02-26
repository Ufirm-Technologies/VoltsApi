namespace vaulterpAPI.Models.Planning
{
    public class ContructionDto
    {
        public int Id { get; set; }
        public int? InternalWoid { get; set; }
        public int? OperationId { get; set; }
        public int? ProductId { get; set; }
        public int? ItemId { get; set; }
        public string? Specification { get; set; }

        public string? gradecode { get; set; }
        public string? Value { get; set; }
        public int? OfficeId { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}