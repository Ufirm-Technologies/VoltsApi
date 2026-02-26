using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models;

namespace vaulterpAPI.Controllers.Inventory
{
    [ApiController]
    [Route("api/inventory/[controller]")]
    public class POController : ControllerBase
    {
        private readonly string _constr;

        public POController(IConfiguration configuration)
        {
            _constr = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet("GetGroupedPurchaseOrderDetails")]
        public async Task<ActionResult<List<PODto>>> GetGroupedPurchaseOrderDetails(
      [FromQuery] int officeId,
      [FromQuery] int? poId = null,
      [FromQuery] int? vendorId = null)
        {
            var flatList = new List<dynamic>();

            var query = @"
        SELECT 
            po.purchase_order_id, 
            po.po_number, 
            po.po_datetime, 
            po.billing_address, 
            po.shipping_address, 
            po.office_id, 
            po.is_approved,
            po.vendor_id, 
            po.vendor_name, 
            po.contact_person, 
            po.contact_number, 
            po.email,
            po.purchase_order_item_id, 
            po.itemid, 
            po.item_name, 
            po.quantity, 
            po.rate, 
            po.line_total,

            COALESCE(SUM(rec.quantity_received), 0) AS quantity_received,
            BOOL_OR(rec.is_rejected) AS is_rejected,
            STRING_AGG(rec.rejection_remarks, '; ') FILTER (WHERE rec.rejection_remarks IS NOT NULL) AS rejection_remarks,
            BOOL_OR(rec.is_completed) AS is_completed

        FROM inventory.vw_purchase_order_details AS po
        LEFT JOIN inventory.scanned_po_data AS rec 
            ON po.purchase_order_item_id = rec.po_item_id
        WHERE po.office_id = @office_id";

            if (poId.HasValue)
                query += " AND po.purchase_order_id = @po_id";
            if (vendorId.HasValue)
                query += " AND po.vendor_id = @vendor_id";

            query += @"
        GROUP BY 
            po.purchase_order_id, 
            po.po_number, 
            po.po_datetime, 
            po.billing_address, 
            po.shipping_address, 
            po.office_id, 
            po.is_approved,
            po.vendor_id, 
            po.vendor_name, 
            po.contact_person, 
            po.contact_number, 
            po.email,
            po.purchase_order_item_id, 
            po.itemid, 
            po.item_name, 
            po.quantity, 
            po.rate, 
            po.line_total";

            await using var conn = new NpgsqlConnection(_constr);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);
            if (poId.HasValue)
                cmd.Parameters.AddWithValue("@po_id", poId.Value);
            if (vendorId.HasValue)
                cmd.Parameters.AddWithValue("@vendor_id", vendorId.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                flatList.Add(new
                {
                    PurchaseOrderId = reader.GetInt32(0),
                    PONumber = reader.GetString(1),
                    PODateTime = reader.GetDateTime(2),
                    BillingAddress = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ShippingAddress = reader.IsDBNull(4) ? null : reader.GetString(4),
                    OfficeId = reader.GetInt32(5),
                    IsApproved = reader.GetBoolean(6),
                    VendorId = reader.GetInt32(7),
                    VendorName = reader.GetString(8),
                    ContactPerson = reader.IsDBNull(9) ? null : reader.GetString(9),
                    ContactNumber = reader.IsDBNull(10) ? null : reader.GetString(10),
                    Email = reader.IsDBNull(11) ? null : reader.GetString(11),
                    PurchaseOrderItemId = reader.GetInt32(12),
                    ItemId = reader.GetInt32(13),
                    ItemName = reader.GetString(14),
                    Quantity = reader.GetDecimal(15),
                    Rate = reader.GetDecimal(16),
                    LineTotal = reader.GetDecimal(17),
                    QuantityReceived = reader.IsDBNull(18) ? 0 : reader.GetInt32(18),
                    IsRejected = reader.IsDBNull(19) ? false : reader.GetBoolean(19),
                    RejectionRemarks = reader.IsDBNull(20) ? null : reader.GetString(20),
                    IsCompleted = reader.IsDBNull(21) ? false : reader.GetBoolean(21)
                });
            }

            var groupedResult = flatList
                .GroupBy(x => x.PurchaseOrderId)
                .Select(g => new PODto
                {
                    PurchaseOrderId = g.Key,
                    PONumber = g.First().PONumber,
                    PODateTime = g.First().PODateTime,
                    BillingAddress = g.First().BillingAddress,
                    ShippingAddress = g.First().ShippingAddress,
                    OfficeId = g.First().OfficeId,
                    IsApproved = g.First().IsApproved,
                    VendorId = g.First().VendorId,
                    VendorName = g.First().VendorName,
                    ContactPerson = g.First().ContactPerson,
                    ContactNumber = g.First().ContactNumber,
                    Email = g.First().Email,
                    Items = g.Select(item => new POItemDto
                    {
                        PurchaseOrderItemId = item.PurchaseOrderItemId,
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        Quantity = item.Quantity,
                        Rate = item.Rate,
                        LineTotal = item.LineTotal,
                        QuantityReceived = item.QuantityReceived,
                        IsRejected = item.IsRejected,
                        RejectionRemarks = item.RejectionRemarks,
                        IsCompleted = item.IsCompleted
                    }).ToList()
                })
                .ToList();

            return Ok(groupedResult);
        }



        //[HttpGet("GetGroupedPurchaseOrderDetails")]
        //public async Task<ActionResult<List<PODto>>> GetGroupedPurchaseOrderDetails([FromQuery] int officeId, [FromQuery] int? poId = null, [FromQuery] int? vendorId = null)
        //{
        //    var flatList = new List<dynamic>();

        //    var query = @"
        //        SELECT 
        //            purchase_order_id, po_number, po_datetime, billing_address, shipping_address, office_id, is_approved,
        //            vendor_id, vendor_name, contact_person, contact_number, email,
        //            purchase_order_item_id, itemid, item_name, quantity, rate, line_total
        //        FROM inventory.vw_purchase_order_details
        //        WHERE office_id = @office_id";

        //    if (poId.HasValue)
        //        query += " AND purchase_order_id = @po_id";
        //    if (vendorId.HasValue)
        //        query += " AND vendor_id = @vendor_id";

        //    await using var conn = new NpgsqlConnection(_constr);
        //    await conn.OpenAsync();

        //    await using var cmd = new NpgsqlCommand(query, conn);
        //    cmd.Parameters.AddWithValue("@office_id", officeId);
        //    if (poId.HasValue)
        //        cmd.Parameters.AddWithValue("@po_id", poId.Value);
        //    if (vendorId.HasValue)
        //        cmd.Parameters.AddWithValue("@vendor_id", vendorId.Value);

        //    await using var reader = await cmd.ExecuteReaderAsync();
        //    while (await reader.ReadAsync())
        //    {
        //        flatList.Add(new
        //        {
        //            PurchaseOrderId = reader.GetInt32(0),
        //            PONumber = reader.GetString(1),
        //            PODateTime = reader.GetDateTime(2),
        //            BillingAddress = reader.IsDBNull(3) ? null : reader.GetString(3),
        //            ShippingAddress = reader.IsDBNull(4) ? null : reader.GetString(4),
        //            OfficeId = reader.GetInt32(5),
        //            IsApproved = reader.GetBoolean(6),
        //            VendorId = reader.GetInt32(7),
        //            VendorName = reader.GetString(8),
        //            ContactPerson = reader.IsDBNull(9) ? null : reader.GetString(9),
        //            ContactNumber = reader.IsDBNull(10) ? null : reader.GetString(10),
        //            Email = reader.IsDBNull(11) ? null : reader.GetString(11),
        //            PurchaseOrderItemId = reader.GetInt32(12),
        //            ItemId = reader.GetInt32(13),
        //            ItemName = reader.GetString(14),
        //            Quantity = reader.GetDecimal(15),
        //            Rate = reader.GetDecimal(16),
        //            LineTotal = reader.GetDecimal(17)
        //        });
        //    }

        //    var groupedResult = flatList
        //        .GroupBy(x => x.PurchaseOrderId)
        //        .Select(g => new PODto
        //        {
        //            PurchaseOrderId = g.Key,
        //            PONumber = g.First().PONumber,
        //            PODateTime = g.First().PODateTime,
        //            BillingAddress = g.First().BillingAddress,
        //            ShippingAddress = g.First().ShippingAddress,
        //            OfficeId = g.First().OfficeId,
        //            IsApproved = g.First().IsApproved,
        //            VendorId = g.First().VendorId,
        //            VendorName = g.First().VendorName,
        //            ContactPerson = g.First().ContactPerson,
        //            ContactNumber = g.First().ContactNumber,
        //            Email = g.First().Email,
        //            Items = g.Select(item => new POItemDto
        //            {
        //                PurchaseOrderItemId = item.PurchaseOrderItemId,
        //                ItemId = item.ItemId,
        //                ItemName = item.ItemName,
        //                Quantity = item.Quantity,
        //                Rate = item.Rate,
        //                LineTotal = item.LineTotal
        //            }).ToList()
        //        })
        //        .ToList();

        //    return Ok(groupedResult);
        //}

        [HttpPost("CreatePurchaseOrders")]
        public async Task<ActionResult<List<object>>> CreatePurchaseOrders([FromBody] List<CreatePurchaseOrderRequestDto> orders)
        {
            if (orders == null || !orders.Any())
                return BadRequest("No purchase orders provided.");

            await using var conn = new NpgsqlConnection(_constr);
            await conn.OpenAsync();
            await using var transaction = await conn.BeginTransactionAsync();

            try
            {
                var createdOrders = new List<object>();

                foreach (var dto in orders)
                {
                    if (dto?.Items == null || !dto.Items.Any())
                        continue;

                    var tempPONumber = $"TEMP-{dto.OfficeId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

                    var insertPOQuery = @"
                        INSERT INTO inventory.purchaseorder 
                            (po_number, vendor_id, billing_address, shipping_address, total_amount, created_by, office_id, is_approved, is_deleted)
                        VALUES 
                            (@po_number, @vendor_id, @billing_address, @shipping_address, @total_amount, @created_by, @office_id, true, false)
                        RETURNING id;";

                    int purchaseOrderId;
                    await using (var poCmd = new NpgsqlCommand(insertPOQuery, conn, transaction))
                    {
                        poCmd.Parameters.AddWithValue("@po_number", tempPONumber);
                        poCmd.Parameters.AddWithValue("@vendor_id", dto.VendorId);
                        poCmd.Parameters.AddWithValue("@billing_address", (object?)dto.BillingAddress ?? DBNull.Value);
                        poCmd.Parameters.AddWithValue("@shipping_address", (object?)dto.ShippingAddress ?? DBNull.Value);
                        poCmd.Parameters.AddWithValue("@total_amount", dto.TotalAmount);
                        poCmd.Parameters.AddWithValue("@created_by", dto.CreatedBy);
                        poCmd.Parameters.AddWithValue("@office_id", dto.OfficeId);

                        purchaseOrderId = (int)(await poCmd.ExecuteScalarAsync())!;
                    }

                    var poNumber = $"PO-{dto.OfficeId}{purchaseOrderId}";
                    var updatePOQuery = "UPDATE inventory.purchaseorder SET po_number = @po_number WHERE id = @id";

                    await using (var updateCmd = new NpgsqlCommand(updatePOQuery, conn, transaction))
                    {
                        updateCmd.Parameters.AddWithValue("@po_number", poNumber);
                        updateCmd.Parameters.AddWithValue("@id", purchaseOrderId);

                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    foreach (var item in dto.Items)
                    {
                        var insertItemQuery = @"
                            INSERT INTO inventory.purchaseorderitems 
                                (purchaseorderid, itemid, quantity, rate, createdby)
                            VALUES (@purchase_order_id, @itemid, @quantity, @rate, @created_by);";

                        await using (var itemCmd = new NpgsqlCommand(insertItemQuery, conn, transaction))
                        {
                            itemCmd.Parameters.AddWithValue("@purchase_order_id", purchaseOrderId);
                            itemCmd.Parameters.AddWithValue("@itemid", item.ItemId);
                            itemCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                            itemCmd.Parameters.AddWithValue("@rate", item.Rate);
                            itemCmd.Parameters.AddWithValue("@created_by", dto.CreatedBy);

                            await itemCmd.ExecuteNonQueryAsync();
                        }
                    }

                    createdOrders.Add(new
                    {
                        PurchaseOrderId = purchaseOrderId,
                        PONumber = poNumber,
                        Message = "Purchase Order created successfully."
                    });
                }

                await transaction.CommitAsync();
                return Ok(createdOrders);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error while creating Purchase Order: {ex.Message}");
            }
        }
    }
}
