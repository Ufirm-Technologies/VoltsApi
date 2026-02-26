namespace vaulterpAPI.Models.Inventory
{
    public class StockDto
    {
        public int stock_id { get; set; }

        public int item_id { get; set; }

        public int office_id { get; set; }

        public int current_qty { get; set; }

        public int min_qty { get; set; }

        public string name { get; set; }            
        public string description { get; set; }
        public int category_id { get; set; }
    }

    public class AddStockDto
    {

        public int item_id { get; set; }

        public int office_id { get; set; }

        public int current_qty { get; set; }
        public int quantity { get; set; }

    }

    public class StockWithVendorDto
    {
        public string po_number { get; set; }
        public int stock_id { get; set; }
        public int item_id { get; set; }
        public int office_id { get; set; }
        public int current_qty { get; set; }
        public int min_qty { get; set; }
        public int quantity { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int category_id { get; set; }
        public string vendor_names { get; set; }
    }

    public class InventoryReturnDTO
    {
        public string PoNumber { get; set; }
        public string ItemName { get; set; }
        public int ItemId { get; set; }
        public int QuantityReturned { get; set; }

        public int OfficeId { get; set; }
        public int Quantity { get; set; }
        public DateTime? ScanDateTime { get; set; }
    }

}
