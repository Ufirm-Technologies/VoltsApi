namespace vaulterpAPI.Models.LWFMaster
{
    public class LwfMaster
    {
        public int? StateId { get; set; }          // stateid (integer)

        public decimal? LwfAmount { get; set; }     // lwfamount (numeric)

        public decimal? EmployeeAmount { get; set; } // employeeamount (numeric)

        public decimal? EmployerAmount { get; set; } // employeramount (numeric)

        public DateTime? CreatedOn { get; set; }     // createdon (timestamp)

        public DateTime? UpdatedOn { get; set; }     // updatedon (timestamp)

        public int? LwfId { get; set; }              // lwfid (integer, identity always)
    }
}
