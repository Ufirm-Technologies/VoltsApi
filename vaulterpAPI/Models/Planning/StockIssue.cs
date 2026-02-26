namespace vaulterpAPI.Models.Planning
{
    public class StockIssueFullDTO
    {
        public int Id { get; set; }
        public int Inwo { get; set; }
        public int? JobcardId { get; set; }
        public string Operation { get; set; }
        public int EmployeeId { get; set; }
        public int ItemId { get; set; }
        public int QuantityIssued { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; }
        public int OfficeId { get; set; }

        // Extra calculated field from scanned_po_data
        public int TotalQuantity { get; set; }
    }

}
