namespace vaulterpAPI.Models.Planning
{
    public class JobCardDto
    {
        public int? Id { get; set; }
        public int? InternalWo { get; set; }
        public string? IsCode { get; set; }
        public DateTime? Date { get; set; }
        public int? ShiftId { get; set; }
        public int? AssetId { get; set; }
        public int? ItemId { get; set; }
        public int? Compected { get; set; }
        public string? NoDiaOfAmWire { get; set; }
        public string? PayOffDNo { get; set; }
        public string? TakeUpDrumSize { get; set; }
        public string? Embrossing { get; set; }
        public string? Remark { get; set; }
        public bool? IsActive { get; set; }
        public int? OfficeId { get; set; }

        public string? GradeCode { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }

        public int? OperationId { get; set; }

        public int? OperatorId { get; set; }
    }
}