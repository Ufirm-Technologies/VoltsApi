namespace vaulterpAPI.Models.Payroll
{
    public class SalaryAllowanceDto
    {
        public int SalaryGroup_ID { get; set; }
        public string? SalaryGroup { get; set; }
        public decimal? BaseSalary { get; set; }
        public int OfficeId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal? TotalWorkingDays { get; set; }
        public decimal? ShiftHours { get; set; }

        public List<AllowanceDeductionDto>? AllowancesDeductions { get; set; }
    }

    public class AllowanceDeductionDto
    {
        // Link row id in salarygroup_allowancedeductionlink
        public int? LinkId { get; set; }

        // AD master id
        public int AD_Id { get; set; }

        // optional friendly name, not stored here (could be fetched when listing from allowance_dedution)
        public string? Name { get; set; }

        public decimal? FixedAmount { get; set; }
        public decimal? CalculatedAmount { get; set; }
        public int? FormulaId { get; set; }
        public string? Formula { get; set; }
        public bool IsActive { get; set; } = true;

        // helper to produce a readable identifier for formulas
        public string ADDisplayNameOrId() => !string.IsNullOrWhiteSpace(Name) ? Name : $"AD{AD_Id}";
    }
}
