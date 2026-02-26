namespace vaulterpAPI.Models
{
    public class PODto
    {
        public int PurchaseOrderId { get; set; }
        public string PONumber { get; set; }
        public DateTime PODateTime { get; set; }
        public string? BillingAddress { get; set; }
        public string? ShippingAddress { get; set; }
        public int OfficeId { get; set; }
        public bool IsApproved { get; set; }

        public int CreatedBy { get; set; }
        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public string ContactPerson { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }
        public decimal TotalAmount { get; set; }


        public List<POItemDto> Items { get; set; } = new List<POItemDto>();
    }

    public class POItemDto
    {
        public int PurchaseOrderItemId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal LineTotal { get; set; }

        public decimal QuantityReceived { get; set; }
        public bool IsRejected { get; set; }
        public string RejectionRemarks { get; set; }
        public bool IsCompleted { get; set; }
    }
    public class CreatePurchaseOrderRequestDto
    {
        public int VendorId { get; set; }
        public string? BillingAddress { get; set; }
        public string? ShippingAddress { get; set; }
        public int CreatedBy { get; set; }
        public int OfficeId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<CreatePurchaseOrderItemDto> Items { get; set; } = new List<CreatePurchaseOrderItemDto>();
    }

    public class CreatePurchaseOrderItemDto
    {
        public int ItemId { get; set; }
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
    }
}
