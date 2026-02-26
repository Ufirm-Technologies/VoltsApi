namespace vaulterpAPI.Models.Planning
{
    public class ItemIssueDto
    {
        public int Id { get; set; }
        public int Inwo { get; set; }
        public int? JobcardId { get; set; }
        public string? Operation { get; set; }
        public int EmployeeId { get; set; }
        public int ItemId { get; set; }
        public int QuantityIssued { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; }
        public int OfficeId { get; set; }
    }
}
