using System;
using System.Collections.Generic;

namespace vaulterpAPI.Models.work_order
{
    public class WorkOrderMaster
    {
        public int Id { get; set; }
        public int PartyId { get; set; }
        public string? PoNo { get; set; }
        public string? BoardName { get; set; }
        public int? PoAmount { get; set; }
        public int? IsActive { get; set; }
        public int OfficeId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        public DateTime? PoDate { get; set; }

        public DateTime? DeliveryDate { get; set; }

        public int? TotalDeliverable { get; set; }

        public int? ProcessId { get; set; }

        // Child Table
        public List<WorkOrderProduct>? Products { get; set; }
    }

    public class WorkOrderProduct
    {
        public int Id { get; set; }
        public int WoId { get; set; } // Foreign key
        public int ProductId { get; set; }
        public int? Quantity { get; set; }
        public string? Store { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class WorkOrderMilestone
    {
        public int Id { get; set; }
        public int Woid { get; set; }
        public DateTime Date { get; set; }
        public int Target { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class InternalWorkOrder
    {
        public int Id { get; set; }
        public int Woid { get; set; }
        public int Quantity { get; set; }
        public int OfficeId { get; set; }
        public DateTime DispatchDate { get; set; }
        public int TotalDeliverable { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string BoardName { get; set; }       // Board name
        public string PoNo { get; set; }            // PO number
        public int PartyId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }

     
    }

}