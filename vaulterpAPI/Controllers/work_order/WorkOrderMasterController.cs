using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Collections.Generic;
using System.Data;
using vaulterpAPI.Models.work_order;

namespace vaulterpAPI.Controllers.work_order
{
    [ApiController]
    [Route("api/work_order/[controller]")]
    public class WorkOrderMasterController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public WorkOrderMasterController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        [HttpPost]
        public IActionResult CreateWorkOrder([FromBody] WorkOrderMaster workOrder)
        {
            using (var conn = new NpgsqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Insert into master
                        
                    var insertMasterCmd = new NpgsqlCommand(@"
INSERT INTO work_order.work_order_master
(party_id, po_no, po_date, board_name, po_amount, is_active, office_id, createdon, createdby, delivery_date, total_deliverables,process_id)
VALUES(@party_id, @po_no, @po_date, @board_name, @po_amount, @is_active, @office_id, @createdon, @createdby, @delivery_date, @total_deliverables,@process_id)
RETURNING id", conn, tran);


                        insertMasterCmd.Parameters.AddWithValue("@party_id", workOrder.PartyId);
                        insertMasterCmd.Parameters.AddWithValue("@po_no", (object?)workOrder.PoNo ?? DBNull.Value);
                        insertMasterCmd.Parameters.AddWithValue("@board_name", (object?)workOrder.BoardName ?? DBNull.Value);
                        insertMasterCmd.Parameters.AddWithValue("@po_amount", (object?)workOrder.PoAmount ?? DBNull.Value);
                        insertMasterCmd.Parameters.AddWithValue("@is_active", workOrder.IsActive ?? 1);
                        insertMasterCmd.Parameters.AddWithValue("@office_id", workOrder.OfficeId);
                        insertMasterCmd.Parameters.AddWithValue("@createdon", (object?)workOrder.CreatedOn ?? DateTime.Now);
                        insertMasterCmd.Parameters.AddWithValue("@createdby", (object?)workOrder.CreatedBy ?? DBNull.Value);
                        insertMasterCmd.Parameters.AddWithValue("@po_date", (object?)workOrder.PoDate ?? DBNull.Value);
                        insertMasterCmd.Parameters.AddWithValue("@delivery_date", (object?)workOrder.DeliveryDate ?? DBNull.Value);
                        insertMasterCmd.Parameters.AddWithValue("@total_deliverables", (object?)workOrder.TotalDeliverable ?? DBNull.Value);
                        insertMasterCmd.Parameters.AddWithValue("@process_id", (object?)workOrder.ProcessId ?? DBNull.Value);

                        int masterId = Convert.ToInt32(insertMasterCmd.ExecuteScalar());

                        // Insert into child table
                        if (workOrder.Products != null && workOrder.Products.Count > 0)
                        {
                            foreach (var product in workOrder.Products)
                            {
                                var insertProductCmd = new NpgsqlCommand(@"
                            INSERT INTO work_order.work_order_product
                            (wo_id, product_id, quantity, store, createdon, createdby)
                            VALUES (@wo_id, @product_id, @quantity, @store, @createdon, @createdby)", conn, tran);

                                insertProductCmd.Parameters.AddWithValue("@wo_id", masterId);
                                insertProductCmd.Parameters.AddWithValue("@product_id", product.ProductId);
                                insertProductCmd.Parameters.AddWithValue("@quantity", (object?)product.Quantity ?? DBNull.Value);
                                insertProductCmd.Parameters.AddWithValue("@store", (object?)product.Store ?? DBNull.Value);
                                insertProductCmd.Parameters.AddWithValue("@createdon", (object?)product.CreatedOn ?? DateTime.Now);
                                insertProductCmd.Parameters.AddWithValue("@createdby", (object?)product.CreatedBy ?? DBNull.Value);


                                insertProductCmd.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                        return Ok(new { message = "Work order and products saved successfully", id = masterId });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, new { message = "Failed to insert", error = ex.Message });
                    }
                }
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateWorkOrder(int id, [FromBody] WorkOrderMaster workOrder)
        {
            using (var conn = new NpgsqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Step 1: Update Master
                        var updateMasterCmd = new NpgsqlCommand(@"
                    UPDATE work_order.work_order_master
                    SET party_id = @party_id,
                        po_no = @po_no,
                        po_date = @po_date,
                        board_name = @board_name,
                        po_amount = @po_amount,
                        is_active = @is_active,
                        office_id = @office_id,
                        updatedon = @updatedon,
                        updatedby = @updatedby,
                        delivery_date = @delivery_date,
                        total_deliverables = @total_deliverables,
                        process_id = @process_id
                    WHERE id = @id", conn, tran);

                        updateMasterCmd.Parameters.AddWithValue("@id", id);
                        updateMasterCmd.Parameters.AddWithValue("@party_id", workOrder.PartyId);
                        updateMasterCmd.Parameters.AddWithValue("@po_no", (object?)workOrder.PoNo ?? DBNull.Value);
                        updateMasterCmd.Parameters.AddWithValue("@po_date", (object?)workOrder.PoDate ?? DBNull.Value);
                        updateMasterCmd.Parameters.AddWithValue("@board_name", (object?)workOrder.BoardName ?? DBNull.Value);
                        updateMasterCmd.Parameters.AddWithValue("@po_amount", (object?)workOrder.PoAmount ?? DBNull.Value);
                        updateMasterCmd.Parameters.AddWithValue("@is_active", workOrder.IsActive ?? 1);
                        updateMasterCmd.Parameters.AddWithValue("@office_id", workOrder.OfficeId);
                        updateMasterCmd.Parameters.AddWithValue("@updatedon", DateTime.Now);
                        updateMasterCmd.Parameters.AddWithValue("@updatedby", (object?)workOrder.UpdatedBy ?? DBNull.Value);
                        updateMasterCmd.Parameters.AddWithValue("@delivery_date", (object?)workOrder.DeliveryDate ?? DBNull.Value);
                        updateMasterCmd.Parameters.AddWithValue("@total_deliverables", (object?)workOrder.TotalDeliverable ?? DBNull.Value);
                        updateMasterCmd.Parameters.AddWithValue("@process_id", (object?)workOrder.ProcessId ?? DBNull.Value);

                        updateMasterCmd.ExecuteNonQuery();

                        // Step 2: Get existing product IDs for this work order
                        var existingProductIds = new List<int>();
                        var getExistingCmd = new NpgsqlCommand("SELECT product_id FROM work_order.work_order_product WHERE wo_id = @wo_id", conn, tran);
                        getExistingCmd.Parameters.AddWithValue("@wo_id", id);
                        using (var reader = getExistingCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                existingProductIds.Add(reader.GetInt32(0));
                            }
                        }

                        // Step 3: Loop through incoming products
                        if (workOrder.Products != null && workOrder.Products.Count > 0)
                        {
                            foreach (var product in workOrder.Products)
                            {
                                if (existingProductIds.Contains(product.ProductId))
                                {
                                    // Update existing product
                                    var updateProductCmd = new NpgsqlCommand(@"
                                UPDATE work_order.work_order_product
                                SET quantity = @quantity,
                                    store = @store,
                                    updatedon = @updatedon,
                                    updatedby = @updatedby
                                WHERE wo_id = @wo_id AND product_id = @product_id", conn, tran);

                                    updateProductCmd.Parameters.AddWithValue("@wo_id", id);
                                    updateProductCmd.Parameters.AddWithValue("@product_id", product.ProductId);
                                    updateProductCmd.Parameters.AddWithValue("@quantity", (object?)product.Quantity ?? DBNull.Value);
                                    updateProductCmd.Parameters.AddWithValue("@store", (object?)product.Store ?? DBNull.Value);
                                    updateProductCmd.Parameters.AddWithValue("@updatedon", DateTime.Now);
                                    updateProductCmd.Parameters.AddWithValue("@updatedby", (object?)product.UpdatedBy ?? DBNull.Value);

                                    updateProductCmd.ExecuteNonQuery();

                                    existingProductIds.Remove(product.ProductId); // mark as processed
                                }
                                else
                                {
                                    // Insert new product
                                    var insertProductCmd = new NpgsqlCommand(@"
                                INSERT INTO work_order.work_order_product
                                (wo_id, product_id, quantity, store, createdon, createdby)
                                VALUES (@wo_id, @product_id, @quantity, @store, @createdon, @createdby)", conn, tran);

                                    insertProductCmd.Parameters.AddWithValue("@wo_id", id);
                                    insertProductCmd.Parameters.AddWithValue("@product_id", product.ProductId);
                                    insertProductCmd.Parameters.AddWithValue("@quantity", (object?)product.Quantity ?? DBNull.Value);
                                    insertProductCmd.Parameters.AddWithValue("@store", (object?)product.Store ?? DBNull.Value);
                                    insertProductCmd.Parameters.AddWithValue("@createdon", DateTime.Now);
                                    insertProductCmd.Parameters.AddWithValue("@createdby", (object?)product.CreatedBy ?? DBNull.Value);

                                    insertProductCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Step 4: Delete removed products
                        foreach (var removeProductId in existingProductIds)
                        {
                            var deleteProductCmd = new NpgsqlCommand("DELETE FROM work_order.work_order_product WHERE wo_id = @wo_id AND product_id = @product_id", conn, tran);
                            deleteProductCmd.Parameters.AddWithValue("@wo_id", id);
                            deleteProductCmd.Parameters.AddWithValue("@product_id", removeProductId);
                            deleteProductCmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        return Ok(new { message = "Work order and products updated successfully", id = id });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, new { message = "Failed to update", error = ex.Message });
                    }
                }
            }
        }

        [HttpDelete("{id}/office/{officeId}")]
        public IActionResult DeleteWorkOrder(int id, int officeId)
        {
            using (var conn = new NpgsqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Step 1: Soft Delete Master
                        var deleteCmd = new NpgsqlCommand(@"
                    UPDATE work_order.work_order_master
                    SET is_active = 0,
                        updatedon = @updatedon
                    WHERE id = @id AND office_id = @officeId", conn, tran);

                        deleteCmd.Parameters.AddWithValue("@id", id);
                        deleteCmd.Parameters.AddWithValue("@officeId", officeId);
                        deleteCmd.Parameters.AddWithValue("@updatedon", DateTime.Now);

                        int affectedRows = deleteCmd.ExecuteNonQuery();

                        if (affectedRows == 0)
                        {
                            tran.Rollback();
                            return NotFound(new { message = "No matching work order found for given ID and OfficeId" });
                        }

                        tran.Commit();
                        return Ok(new { message = "Work order soft-deleted successfully", id = id });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, new { message = "Failed to delete work order", error = ex.Message });
                    }
                }
            }
        }
        [HttpGet("office/{officeId}")]
        public IActionResult GetWorkOrdersByOfficeId(int officeId)
        {
            List<WorkOrderMaster> workOrders = new List<WorkOrderMaster>();
            List<WorkOrderProduct> allProducts = new List<WorkOrderProduct>();

            try
            {
                using (var conn = new NpgsqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    // Step 1: Get master records
                    var cmdMaster = new NpgsqlCommand(@"
                SELECT * FROM work_order.work_order_master 
                WHERE office_id = @officeId AND is_active = 1", conn);
                    cmdMaster.Parameters.AddWithValue("@officeId", officeId);

                    using (var reader = cmdMaster.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var master = new WorkOrderMaster
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                PartyId = reader.GetInt32(reader.GetOrdinal("party_id")),
                                PoNo = reader["po_no"]?.ToString(),
                                PoDate = reader["po_date"] as DateTime?,
                                BoardName = reader["board_name"]?.ToString(),
                                PoAmount = reader["po_amount"] as int?,
                                IsActive = reader["is_active"] as int?,
                                OfficeId = reader.GetInt32(reader.GetOrdinal("office_id")),
                                CreatedOn = reader["createdon"] as DateTime?,
                                CreatedBy = reader["createdby"] as int?,
                                UpdatedOn = reader["updatedon"] as DateTime?,
                                UpdatedBy = reader["updatedby"] as int?,
                                Products = new List<WorkOrderProduct>(),
                                DeliveryDate = reader["delivery_date"] as DateTime?,
                                TotalDeliverable = reader["total_deliverables"] as int?,
                                ProcessId = reader["process_id"] as int?
                            };
                            workOrders.Add(master);
                        }
                    }

                    // Step 2: Get child products if any
                    if (workOrders.Count > 0)
                    {
                        var woIds = workOrders.Select(m => m.Id).ToArray();

                        var cmdProduct = new NpgsqlCommand(@"
                    SELECT * FROM work_order.work_order_product 
                    WHERE wo_id = ANY(@woIds)", conn);
                        cmdProduct.Parameters.AddWithValue("@woIds", woIds);

                        using (var reader = cmdProduct.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var product = new WorkOrderProduct
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    WoId = reader.GetInt32(reader.GetOrdinal("wo_id")),
                                    ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
                                    Quantity = reader["quantity"] as int?,
                                    Store = reader["store"]?.ToString(),
                                    CreatedOn = reader["createdon"] as DateTime?,
                                    CreatedBy = reader["createdby"] as int?,
                                    UpdatedOn = reader["updatedon"] as DateTime?,
                                    UpdatedBy = reader["updatedby"] as int?
                                };

                                allProducts.Add(product); // capture all products

                                var parent = workOrders.FirstOrDefault(w => w.Id == product.WoId);
                                if (parent != null)
                                {
                                    parent.Products.Add(product);
                                }
                            }
                        }
                    }
                }

                return Ok(workOrders);
            }
            catch (Exception ex)
            {
                string errorDetails = $"Error: {ex.Message} | Fetched WorkOrderMaster count: {workOrders.Count}, WorkOrderProduct count: {allProducts.Count}";
                return StatusCode(500, errorDetails);
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetWorkOrderById(int id)
        {
            WorkOrderMaster workOrder = null;

            try
            {
                using (var conn = new NpgsqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    // Step 1: Get master record
                    var cmdMaster = new NpgsqlCommand(@"
                SELECT * FROM work_order.work_order_master
                WHERE id = @id AND is_active = 1", conn);
                    cmdMaster.Parameters.AddWithValue("@id", id);

                    using (var reader = cmdMaster.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            workOrder = new WorkOrderMaster
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                PartyId = reader.GetInt32(reader.GetOrdinal("party_id")),
                                PoNo = reader["po_no"]?.ToString(),
                                PoDate = reader["po_date"] as DateTime?,
                                BoardName = reader["board_name"]?.ToString(),
                                PoAmount = reader["po_amount"] as int?,
                                IsActive = reader["is_active"] as int?,
                                OfficeId = reader.GetInt32(reader.GetOrdinal("office_id")),
                                CreatedOn = reader["createdon"] as DateTime?,
                                CreatedBy = reader["createdby"] as int?,
                                UpdatedOn = reader["updatedon"] as DateTime?,
                                UpdatedBy = reader["updatedby"] as int?,
                                DeliveryDate = reader["delivery_date"] as DateTime?,
                                TotalDeliverable = reader["total_deliverables"] as int?,
                                ProcessId= reader["process_id"] as int?,
                                Products = new List<WorkOrderProduct>()
                            };
                        }
                    }

                    // Step 2: If master found, get child products
                    if (workOrder != null)
                    {
                        var cmdProduct = new NpgsqlCommand(@"
                    SELECT * FROM work_order.work_order_product
                    WHERE wo_id = @wo_id", conn);
                        cmdProduct.Parameters.AddWithValue("@wo_id", id);

                        using (var reader = cmdProduct.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var product = new WorkOrderProduct
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    WoId = reader.GetInt32(reader.GetOrdinal("wo_id")),
                                    ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
                                    Quantity = reader["quantity"] as int?,
                                    Store = reader["store"]?.ToString(),
                                    CreatedOn = reader["createdon"] as DateTime?,
                                    CreatedBy = reader["createdby"] as int?,
                                    UpdatedOn = reader["updatedon"] as DateTime?,
                                    UpdatedBy = reader["updatedby"] as int?
                                };

                                workOrder.Products.Add(product);
                            }
                        }
                    }
                }

                if (workOrder == null)
                    return NotFound(new { message = "Work order not found" });

                return Ok(workOrder);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching work order", error = ex.Message });
            }
        }

    }
}