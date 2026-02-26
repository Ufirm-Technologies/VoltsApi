using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace vaulterpAPI.Models.Asset
{
    public class AssetModel
    {
        public int Id { get; set; }
        public string AssetName { get; set; }

        public int MaintenanceId { get; set; }
    }
    public class CheckInRequest
    {
        [FromForm(Name = "Maintenance")]
        public string MaintenanceJson { get; set; } = "";

        [NotMapped]
        public AssetSpareMaintenanceModel Maintenance =>
            string.IsNullOrEmpty(MaintenanceJson)
                ? new AssetSpareMaintenanceModel()
                : JsonConvert.DeserializeObject<AssetSpareMaintenanceModel>(MaintenanceJson);

        public bool ReplacementRequired { get; set; }

        [FromForm(Name = "Replacement")]
        public string? ReplacementJson { get; set; }

        [NotMapped]
        public AssetSpareReplacementRequest? Replacement =>
            string.IsNullOrEmpty(ReplacementJson)
                ? null
                : JsonConvert.DeserializeObject<AssetSpareReplacementRequest>(ReplacementJson);

        public int? ApproverId { get; set; }
    }

    public class AssetSpareMaintenanceModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SpareId { get; set; }

        [Required]
        public int AssetId { get; set; }

        // Usage Tracking
        public int? IssuedTo { get; set; }
        public int? IssuedBy { get; set; }

        [Required]
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        public DateTime? ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }

        // Cost & Warranty
        public bool UnderWarranty { get; set; } = false;
        public DateTime? WarrantyExpiry { get; set; }

        public decimal ReplacementCost { get; set; } = 0.00M;
        public decimal ScrapValue { get; set; } = 0.00M;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal NetCost { get; set; }

        // Condition
        public string ReturnCondition { get; set; }
        public int Quantity { get; set; } = 1;

        // Workflow
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public string Purpose { get; set; }
        public string OutFrom { get; set; }
        public string SentTo { get; set; }

        // Images stored as bytea
        public string? ImageOutBase64 { get; set; }
        public string? ImageInBase64 { get; set; }

        public string Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Base64 helper properties for API input/output
    }

    public class AssetSpareReplacementRequest
    {
        public int OldSpareId { get; set; }
        public int AssetId { get; set; }
        public bool UseExistingSpare { get; set; } = false; // true if using spare already in master
        public int? NewSpareId { get; set; } // only if UseExistingSpare = true
        public AssetSpareMaster NewSpare { get; set; } // only if adding a new spare
        public decimal ScrapValue { get; set; }
        public decimal ReplacementCost { get; set; }
        public string Remarks { get; set; }
    }


    public class AssetSpareApproval
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MaintenanceId { get; set; }

        [Required, MaxLength(20)]
        public string ActionType { get; set; }

        public int? ApproverId { get; set; }
        public int ApprovalLevel { get; set; } = 1;

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public string Comments { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    public class AssetSpareMaster
    {
        [Key]
        public int SpareId { get; set; }
        public string SpareCode { get; set; }
        public string SpareName { get; set; }
        public string PartNumber { get; set; }
        public string Category { get; set; }
        public string Specification { get; set; }
        public string UnitOfMeasure { get; set; }
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public int ReorderQuantity { get; set; }
        public string Location { get; set; }
        public int? LinkedAssetId { get; set; }
        public string VendorName { get; set; }
        public decimal PurchaseRate { get; set; }
        public decimal AverageCost { get; set; }
        public int LeadTimeDays { get; set; }
        public string Criticality { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public string Remarks { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsNew { get; set; } = true;
    }
}
