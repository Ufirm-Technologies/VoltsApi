namespace vaulterpAPI.Models.Asset
{
    public class AssetDto
    {
        public int AssetId { get; set; }
        public string AssetCode { get; set; }
        public string AssetName { get; set; }
        public int AssetTypeId { get; set; }
        public int OfficeId { get; set; }
        public string? ModelNumber { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }

        public DateTime? NextServiceDate { get; set; }

        public DateTime? LastServiceDate { get; set; }

        public string? Manufacturer { get; set; }
        public string? Supplier { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class AssetOperationMappingRequest
    {
        public int AssetId { get; set; }
        public List<int> OperationIds { get; set; } = new();
        public int UpdatedBy { get; set; }
    }

    public class CheckOutModel
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssigneeName { get; set; }
        public string Purpose { get; set; }
        public DateTime? CheckOutDateTime { get; set; }

        public string ApprovedBy { get; set; }
        public string OutFrom { get; set; }
        public string SentTo { get; set; }
        public string ImageOut { get; set; }

        // Approval
        public string CheckoutApprovalStatus { get; set; } = "Pending";
        public string CheckoutApprovedBy { get; set; }
        public DateTime? CheckoutApprovalDateTime { get; set; }

        public List<SpareCheckOutModel> SpareFields { get; set; }
    }

    public class SpareCheckOutModel
    {
        public string SpareName { get; set; }
        public bool IsNew { get; set; } = false;
        public decimal? SpareAmount { get; set; }
        public DateTime? TentativeReturnDate { get; set; }

        // Optional spare master fields if IsNew
        public string SpareCode { get; set; }
        public string PartNumber { get; set; }
        public string Category { get; set; }
        public string Specification { get; set; }
        public string UnitOfMeasure { get; set; } = "Piece";
        public int? CurrentStock { get; set; }
        public string VendorName { get; set; }
        public decimal? PurchaseRate { get; set; }
        public decimal? AverageCost { get; set; }
        public int? LeadTimeDays { get; set; }
        public string Criticality { get; set; } = "Medium";
        public DateTime? WarrantyExpiry { get; set; }
        public string Remarks { get; set; }
    }

    public class AssetSpareRequest
    {
        public string? SpareCode { get; set; }
        public string? SpareName { get; set; }
        public string? PartNumber { get; set; }
        public string? Category { get; set; }
        public string? Specification { get; set; }
        public string? UnitOfMeasure { get; set; }
        public int? CurrentStock { get; set; }
        public int? ReorderLevel { get; set; }
        public int? ReorderQuantity { get; set; }
        public string? Location { get; set; }
        public string? VendorName { get; set; }
        public decimal? PurchaseRate { get; set; }
        public decimal? AverageCost { get; set; }
        public int? LeadTimeDays { get; set; }
        public string? Criticality { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public string? Remarks { get; set; }

        public int? LinkedAssetId { get; set; }  
    }

    public class CheckInModel
    {
        public int CheckoutId { get; set; }
        public int AssetId { get; set; }
        public string ReturnedBy { get; set; }
        public DateTime? ReturnDateTime { get; set; }
        public string ReturnCondition { get; set; }
        public string ImageIn { get; set; }

        // Approval
        public string ReturnApprovalStatus { get; set; } = "Pending";
        public string ReturnApprovedBy { get; set; }
        public DateTime? ReturnApprovalDateTime { get; set; }

        public List<SpareCheckInModel> SpareFields { get; set; }
    }

    public class SpareCheckInModel
    {
        public string SpareName { get; set; }
        public decimal? SpareAmount { get; set; }
        public bool? RepairNeeded { get; set; }
        public bool? IsScrap { get; set; }
        public int? ScrapOldSpareValue { get; set; }
    }

    public class AssetTransaction
    {
        public int CheckoutId { get; set; }
        public int AssetId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int OfficeId { get; set; }

        public string sentTo { get; set; }

        public string OutFrom { get; set; }
        public DateTime? CheckOutDateTime { get; set; }
        public DateTime? TentativeReturnDate { get; set; }
        public string Purpose { get; set; }
        public string AssigneeName { get; set; }

        // Checkout approval
        public string CheckoutApprovalStatus { get; set; }
        public string CheckoutApprovedBy { get; set; }

        public string ReturnedBy { get; set; }
        public DateTime? ReturnDateNullable { get; set; }
        public string ReturnCondition { get; set; }
        public bool? RepairNeeded { get; set; }
        public bool? IsScrap { get; set; }
        public int? ScrapOldSpareValue { get; set; }

        // Return approval
        public string ReturnApprovalStatus { get; set; }
        public string ReturnApprovedBy { get; set; }

        public byte[] ImageOut { get; set; }
        public byte[] ImageIn { get; set; }

        public string SpareName { get; set; }
        public decimal? SpareAmount { get; set; }
    }
    public class CheckInApprovalModel
    {
        public string ApprovedBy { get; set; }

        // Spares to be added only if indicated by frontend
        public List<SpareCheckOutModel> SpareFields { get; set; }
    }
    public class ApproveRequest
    {
        public string ApprovedBy { get; set; }
    }

}
