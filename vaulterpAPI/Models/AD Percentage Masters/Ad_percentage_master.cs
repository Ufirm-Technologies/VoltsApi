namespace vaulterpAPI.Models.Payroll
{
    public class ADPercentageMaster
    {
        public int? Id { get; set; }
        public string? AD_Name { get; set; }
        public decimal? Percentage { get; set; }
        public int? IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}

