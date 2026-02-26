namespace vaulterpAPI.Models.Employee
{
    public class EmployeeDto
    {
        public int? EmployeeId { get; set; } 
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int? OfficeId { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public int? RoleId { get; set; }
        public DateTime? JoiningDate { get; set; }
        public DateTime? LeavingDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string ProfileImageUrl { get; set; }
        public int? CreatedBy { get; set; }
        public string EmploymentType { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string PanCard { get; set; }
        public string AadharCard { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Gender { get; set; }
        public string ProfileImageName { get; set; }

        public string Latitude { get; set; }
        public string Longitude { get; set; }

        public int? ReportsTo { get; set; }

        public string? Username { get; set; }

        public string? OfficeName { get; set; }

        // Navigation properties
        public List<WorkHistoryDto> WorkHistory { get; set; } = new();
        public List<BankDetailsDto> BankDetails { get; set; } = new();
    }

    // Work History DTO
    public class WorkHistoryDto
    {
        public string CompanyName { get; set; }
        public string Role { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public DateTime? RelievingDate { get; set; }
        public bool ThirdPartyVerification { get; set; } = false;
        public int? OfficeId { get; set; }
        public IFormFile[] ResumeFiles { get; set; } 
        
        public bool IsActive { get; set; }// For file uploads
    }

    // Bank Details DTO
    public class BankDetailsDto
    {
        public string BankName { get; set; }
        public string PanNo { get; set; }
        public string BankAccNo { get; set; }            // changed to long to support large account numbers
        public string UanNo { get; set; }
        public string IfscCode { get; set; }
        public int OfficeId { get; set; }
        public bool IsActive { get; set; }// For file uploads
    }


    public class OperationDto
    {
        public int OperationId { get; set; }
        public string OperationName { get; set; }
        public string Description { get; set; }
        public int OfficeId { get; set; }
        public bool IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    // ✅ DTO for mapping
    public class EmpOpsDto
    {
        public int EmployeeId { get; set; }
        public int OperationId { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class EmployeeOperationMappingRequest
    {
        public int EmployeeId { get; set; }
        public List<int> OperationIds { get; set; } = new();
        public int UpdatedBy { get; set; }
    }
    public class CreateOperationRequest
    {
        public string OperationName { get; set; } = "";
        public string? Description { get; set; }

        public string OperationCode { get; set; } = "";
        public int OfficeId { get; set; }
        public int CreatedBy { get; set; }
    }

    public class EmployeeShiftDto
    {
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; } // for response
        public int ShiftId { get; set; }
        public string? ShiftName { get; set; } // for response
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public bool IsActive { get; set; } = true;
        public string? MobileNo { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
