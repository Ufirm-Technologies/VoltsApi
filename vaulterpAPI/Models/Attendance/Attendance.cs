namespace vaulterpAPI.Models.Attendance
{
    public class ShiftDto
    {
        public int ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public string? ShiftCode { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int OfficeId { get; set; }
    }

    public class AttendanceSummaryDto
    {
        public DateTime PunchDate { get; set; }
        public string EmployeeName { get; set; }
        public string DayOfWeek { get; set; }
        public string? MinCheckIn { get; set; }
        public string? MaxCheckOut { get; set; }
        public string? TotalWorkingTime { get; set; }
        public string Status { get; set; }
    }

    public class LeaveRequestDTO
    {
        public int LeaveId { get; set; }
        public string? MobileNo { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; }
        public DateTime AppliedOn { get; set; }
        public string? LeaveType { get; set; }
        public int EmployeeId { get; set; }
        public string? RejectionRemarks { get; set; }
        public int LeaveTypeId { get; set; }
        public decimal LeaveCount { get; set; }
        public string? EmployeeName { get; set; }
        public int OfficeId { get; set; }
    }

    public class LeaveRequestCreateModel
    {
        public string? MobileNo { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? Reason { get; set; }
        public string? LeaveType { get; set; }
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public decimal LeaveCount { get; set; }
    }

    public class RejectLeaveModel
    {
        public string? Remarks { get; set; }
    }

    public class HolidayDTO
    {
        public DateTime HolidayDate { get; set; }
        public string Description { get; set; }
    }

    public class LeaveMasterDto
    {
        public int Id { get; set; }
        public int? OfficeId { get; set; }
        public string LeaveType { get; set; }
        public string LeaveDescription { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public bool IsActive { get; set; }
    }

    public class EmployeeLeaveDto
    {
        public long Id { get; set; }
        public int OfficeId { get; set; }
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public decimal Balance { get; set; }
        public int FinancialYear { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public bool IsActive { get; set; }
    }

    public class LeaveBalanceDTO
    {
        public int LeaveTypeId { get; set; }
        public string LeaveType { get; set; }
        public decimal TakenLeaves { get; set; }
        public decimal RemainingLeaves { get; set; }
    }

    public class ManualAttendanceDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime PunchDate { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public int? GateNo { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public string? MobileNo { get; set; }
        public int? EmpId { get; set; }
        public string? Status { get; set; }
        public byte[]? ImageFile { get; set; }
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public string? RejectionRemark { get; set; }
        public int OfficeId { get; set; }
    }

}
