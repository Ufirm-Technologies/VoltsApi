namespace vaulterpAPI.Models.work_order
{
    public class ProductMaster
    {
        public int id { get; set; }
        public string product_name { get; set; }
        public string? description { get; set; }
        public int? rate { get; set; }
        public string? unit { get; set; }
        public int? is_active { get; set; }
        public int? office_id { get; set; }
        public DateTime? createdon { get; set; }
        public int? createdby { get; set; }
        public DateTime? updatedon { get; set; }
        public int? updatedby { get; set; }
    }
}
