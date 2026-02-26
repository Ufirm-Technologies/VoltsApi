using Microsoft.VisualBasic;

namespace vaulterpAPI.Models.Asset
{
    public class ApproveRejectRequest
    {
        public int RecordId { get; set; }
        public bool IsApproved { get; set; } = false;
        public bool IsRejected { get; set; } = false;
        public string? RejectionRemark { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
    }

    public class AssetServiceRecordDto
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public string? ServiceDate { get; set; }
        public string? NextServiceDate { get; set; }
        public string? Image { get; set; }
        public string? Remark { get; set; }
        public string? ServiceDoc { get; set; }
        public int ServiceCost { get; set; }
        public string? ServicedBy { get; set; }
        public string? ApprovedBy { get; set; }
        public string? CreatedBy { get; set; }
        public TimeSpan? Duration { get; set; }  
        
        public int Days { get; set; }// Changed from DateInterval
        public bool? IsRejected { get; set; } = false;
        public bool? IsApproved { get; set; } = false;
        public string? RejectionRemark { get; set; }
    }

    public class AssetServiceRecordForm
    {
        public int AssetId { get; set; }
        public DateTime? ServiceDate { get; set; }
        public DateTime? NextServiceDate { get; set; }
        public string Remark { get; set; }
        public int ServiceCost { get; set; }
        public string ServicedBy { get; set; }
        public string ApprovedBy { get; set; }
        public string CreatedBy { get; set; }

        public TimeSpan? Duration { get; set; } 
        
        public int Days { get; set; }// Changed from DateInterval
        public bool? IsRejected { get; set; } = false;
        public bool? IsApproved { get; set; } = false;
        public string? RejectionRemark { get; set; }
        public IFormFile Image { get; set; }
        public IFormFile ServiceDoc { get; set; }
    }


}
