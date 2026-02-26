namespace vaulterpAPI.Models.CountryMaster
{
    public class CountryMaster
    {
        public int? CountryId { get; set; }          // integer
        public string? CName { get; set; }           // character varying
        public DateTime? CreatedOn { get; set; }     // timestamp
        public DateTime? UpdatedOn { get; set; }     // timestamp
    }
}

