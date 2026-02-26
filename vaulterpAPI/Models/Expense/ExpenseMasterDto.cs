namespace vaulterpAPI.Models.Expense
{
    public class ExpenseRequestDto
    {
        public string Type { get; set; } = string.Empty;
        public List<string> SubTypes { get; set; } = new();
        public int OfficeId { get; set; }
        public bool IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
    }

    public class ExpenseResponseDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public List<string> SubTypes { get; set; } = new();
        public int OfficeId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class ExpenseGroupDto
    {
        public string Type { get; set; }
        public List<string> SubTypes { get; set; }
        public int OfficeId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class ExpenseUpdateDto
    {
        public string Type { get; set; }
        public List<string> SubTypes { get; set; }
        public int OfficeId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
    }

    public class ExpenseMaster
    {
        public int Id { get; set; }
        public string ExpenseType { get; set; }
        public string? ExpenseSubtype { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? BillImage { get; set; }   // base64 string for API
        public int OfficeId { get; set; }
        public bool IsActive { get; set; }
        public int? CreatedBy { get; set; }      // userId
        public DateTime? CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }      // userId
        public DateTime? UpdatedOn { get; set; }
    }
}
