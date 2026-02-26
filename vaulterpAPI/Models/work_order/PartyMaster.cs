namespace vaulterpAPI.Models.work_order
{
    public class PartyMaster
    {
        public int id { get; set; }
        public int office_id { get; set; }
        public string? name { get; set; }
        public string? contact_person { get; set; }
        public string? contact_number { get; set; }
        public string? email { get; set; }
        public string? address { get; set; }
        public string? gst_number { get; set; }
        public string? pan_number { get; set; }
        public bool is_approved { get; set; }
        public int? approved_by { get; set; }
        public int? created_by { get; set; }
        public DateTime created_on { get; set; }
        public bool is_active { get; set; }
        public string? pan_url { get; set; }
        public string? gst_certificate_url { get; set; }
        public string? company_brochure_url { get; set; }
        public string? website_url { get; set; }
    }
}
