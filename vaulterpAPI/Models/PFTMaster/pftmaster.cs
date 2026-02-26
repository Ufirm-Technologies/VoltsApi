namespace vaulterpAPI.Models.PFTMaster
{
    public class PftMaster
    {
        public int? PftId { get; set; }            // pftid (identity always)

        public int? StateId { get; set; }          // stateid (integer)

        public decimal? AmountFrom { get; set; }    // amountfrom (numeric)

        public decimal? AmountTo { get; set; }      // amountto (numeric)

        public decimal? PftAmount { get; set; }     // pftamount (numeric)

        public DateTime? CreatedOn { get; set; }    // createdon (timestamp)

        public DateTime? UpdatedOn { get; set; }    // updatedon (t
    }
}