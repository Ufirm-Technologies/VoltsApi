namespace vaulterpAPI.Models.Planning
{
    public class DailyPlanningSheetDto
    {
        public int Id { get; set; }
        public int OfficeId { get; set; }
        public DateTime PlanDate { get; set; }
        public int EmployeeId { get; set; }
        public int OperationId { get; set; }
        public int AssetId { get; set; }
        public int InternalWorkOrderId { get; set; }
        public int ShiftId { get; set; }
        public int Manpower { get; set; }
        public int? Target { get; set; }
        public int? Achieved { get; set; }
        public string? Backfeed { get; set; }
        public string? Remarks { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public bool IsActive { get; set; }
    }

}
