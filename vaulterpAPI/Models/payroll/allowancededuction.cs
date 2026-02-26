namespace vaulterpAPI.Models.Payroll
{
    public class AllowanceDedution
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public int OfficeId { get; set; }

        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        public bool IsActive { get; set; }
    }
}
