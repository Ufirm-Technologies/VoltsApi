namespace vaulterpAPI.Models.Payroll
{
    public class FormulaMaster
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Formula { get; set; }
        public decimal? FixedValue { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? IsActive { get; set; }   
        public int? OfficeId { get; set; }
    }
}
