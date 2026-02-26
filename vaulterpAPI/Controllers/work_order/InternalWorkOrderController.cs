using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.work_order;

namespace vaulterpAPI.Controllers.work_order
{
    [ApiController]
    [Route("api/work_order/[controller]")]
    public class InternalWorkOrderController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public InternalWorkOrderController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // POST
        [HttpPost]
        public IActionResult Create([FromBody] InternalWorkOrder model)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            // Fetch DeliveryDate & TotalDeliverable from work_order_master
            var fetchCmd = new NpgsqlCommand(@"
        SELECT delivery_date, total_deliverables
        FROM work_order.work_order_master
        WHERE id = @woid", conn);

            fetchCmd.Parameters.AddWithValue("@woid", model.Woid);

            using var reader = fetchCmd.ExecuteReader();
            if (!reader.Read())
                return NotFound(new { message = "Work Order not found" });

            var deliveryDate = reader.GetDateTime(reader.GetOrdinal("delivery_date"));
            var totalDeliverable = reader.GetInt32(reader.GetOrdinal("total_deliverables"));
            reader.Close();

            // Insert into internal_work_order
            var insertCmd = new NpgsqlCommand(@"
        INSERT INTO work_order.internal_work_order
        (woid, quantity, dispatchdate, totaldeliverable, deliverydate,
         is_active, created_on, created_by, updated_on, updated_by)
        VALUES (@woid, @quantity, @dispatchdate, @totaldeliverable, @deliverydate,
                @is_active, @createdon, @createdby, @updatedon, @updatedby)
        RETURNING id", conn);

            insertCmd.Parameters.AddWithValue("@woid", model.Woid);
            insertCmd.Parameters.AddWithValue("@quantity", model.Quantity);
            insertCmd.Parameters.AddWithValue("@dispatchdate", model.DispatchDate);
            insertCmd.Parameters.AddWithValue("@totaldeliverable", totalDeliverable);
            insertCmd.Parameters.AddWithValue("@deliverydate", deliveryDate);
            insertCmd.Parameters.AddWithValue("@is_active", model.IsActive);
            insertCmd.Parameters.AddWithValue("@createdon", model.CreatedOn ?? DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("@createdby", model.CreatedBy ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("@updatedon", model.UpdatedOn ?? DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("@updatedby", model.UpdatedBy ?? (object)DBNull.Value);

            var id = Convert.ToInt32(insertCmd.ExecuteScalar());
            return Ok(new { message = "Internal Work Order created", id });
        }


        // GET by Work Order ID
        [HttpGet("woid/{woid}")]
        public IActionResult GetByWorkOrder(int woid)
        {
            var list = new List<InternalWorkOrder>();
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            var cmd = new NpgsqlCommand(@"
SELECT i.id, i.woid, i.quantity, i.dispatchdate, 
       w.total_deliverables AS total_deliverable, 
       w.delivery_date AS delivery_date,
       i.is_active, i.created_on, i.created_by, i.updated_on, i.updated_by
FROM work_order.internal_work_order i
JOIN work_order.work_order_master w ON i.woid = w.id
WHERE i.woid = @woid AND i.is_active = true", conn);


            cmd.Parameters.AddWithValue("@woid", woid);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new InternalWorkOrder
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Woid = reader.GetInt32(reader.GetOrdinal("woid")),
                    Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                    DispatchDate = reader.GetDateTime(reader.GetOrdinal("dispatchdate")),
                    TotalDeliverable = reader.GetInt32(reader.GetOrdinal("total_deliverable")),
                    DeliveryDate = reader.GetDateTime(reader.GetOrdinal("delivery_date")),
                    IsActive = (bool)reader["is_active"],
                    CreatedOn = reader["created_on"] as DateTime?,
                    CreatedBy = reader["created_by"] as string,
                    UpdatedOn = reader["updated_on"] as DateTime?,
                    UpdatedBy = reader["updated_by"] as string
                });
            }

            return Ok(list);
        }


        // PUT
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] InternalWorkOrder model)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            var cmd = new NpgsqlCommand(@"
                UPDATE work_order.internal_work_order
                SET quantity = @quantity,
                    dispatchdate = @dispatchdate,
                    totaldeliverable = @totaldeliverable,
                    deliverydate = @deliverydate,
                    updated_by = @updatedby,
                    updated_on = @updatedon,
                    is_active = @is_active
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@quantity", model.Quantity);
            cmd.Parameters.AddWithValue("@dispatchdate", model.DispatchDate);
            cmd.Parameters.AddWithValue("@totaldeliverable", model.TotalDeliverable);
            cmd.Parameters.AddWithValue("@deliverydate", model.DeliveryDate);
            cmd.Parameters.AddWithValue("@updatedby", model.UpdatedBy ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedon", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@is_active", model.IsActive);

            int affected = cmd.ExecuteNonQuery();
            if (affected == 0) return NotFound(new { message = "Internal Work Order not found" });

            return Ok(new { message = "Internal Work Order updated" });
        }

        // GET by Work Order ID and Office ID
        [HttpGet("office/{officeId}")]
        public async Task<IActionResult> GetByWorkOrderAndOffice(int officeId)
        {
            var list = new List<InternalWorkOrder>();
            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(@"
            SELECT i.id, i.woid, i.quantity, i.dispatchdate, 
                   w.total_deliverables AS total_deliverable, 
                   w.delivery_date AS delivery_date, w.office_id, w.board_name,w.po_no,w.party_id,
                   i.is_active, i.created_on, i.created_by, i.updated_on, i.updated_by
            FROM work_order.internal_work_order i
            JOIN work_order.work_order_master w 
                 ON i.woid = w.id
            WHERE w.office_id = @officeId
              AND i.is_active = true", conn);

                cmd.Parameters.AddWithValue("@officeId", officeId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new InternalWorkOrder
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Woid = reader.GetInt32(reader.GetOrdinal("woid")),
                        Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                        DispatchDate = reader.GetDateTime(reader.GetOrdinal("dispatchdate")),
                        TotalDeliverable = reader.GetInt32(reader.GetOrdinal("total_deliverable")),
                        DeliveryDate = reader.GetDateTime(reader.GetOrdinal("delivery_date")),
                        OfficeId = reader.GetInt32(reader.GetOrdinal("office_id")),
                        BoardName = reader.GetString(reader.GetOrdinal("board_name")),
                        PoNo = reader.GetString(reader.GetOrdinal("po_no")),
                        PartyId = reader.GetInt32(reader.GetOrdinal("party_id")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                        CreatedOn = reader["created_on"] as DateTime?,
                        CreatedBy = reader["created_by"] as string,
                        UpdatedOn = reader["updated_on"] as DateTime?,
                        UpdatedBy = reader["updated_by"] as string
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }



        // DELETE (Soft Delete)
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            var cmd = new NpgsqlCommand(@"
                UPDATE work_order.internal_work_order
                SET is_active = false,
                    updated_on = @updatedon
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@updatedon", DateTime.UtcNow);

            int affected = cmd.ExecuteNonQuery();
            if (affected == 0) return NotFound(new { message = "Internal Work Order not found" });

            return Ok(new { message = "Internal Work Order soft-deleted" });
        }
    

    [HttpGet("products/{inwoId}")]
        public IActionResult GetProductsByInternalWorkOrder(int inwoId)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT wop.product_id
                        FROM work_order.internal_work_order inwo
                        JOIN work_order.work_order_master wo ON inwo.woid = wo.id
                        JOIN work_order.work_order_product wop ON wo.id = wop.wo_id
                        WHERE inwo.id = @inwoId;
                    ";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@inwoId", inwoId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            var products = new List<int>();
                            while (reader.Read())
                            {
                                products.Add(reader.GetInt32(0));
                            }

                            if (products.Count == 0)
                                return NotFound(new { Message = "No products found for the given Internal Work Order." });

                            return Ok(products);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error fetching products.", Error = ex.Message });
            }
        }
        [HttpGet("operations/{inwoId}")]
        public IActionResult GetOperationsByInternalWorkOrder(int inwoId)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT o.operation_id, o.operation_name 
                FROM master.operation_master o
                INNER JOIN work_order.internal_work_order inwo ON inwo.id = @inwoId
                INNER JOIN work_order.work_order_master wo ON wo.id = inwo.woid
                INNER JOIN planning.process_operations po ON po.process_id = wo.process_id
                WHERE o.operation_id = po.operation_id
                  AND inwo.is_active = true;
            ";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@inwoId", inwoId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            var operations = new List<object>();
                            while (reader.Read())
                            {
                                operations.Add(new
                                {
                                    OperationId = reader.GetInt32(reader.GetOrdinal("operation_id")),
                                    OperationName = reader.GetString(reader.GetOrdinal("operation_name"))
                                });
                            }

                            if (operations.Count == 0)
                                return NotFound(new { Message = "No operations found for the given Internal Work Order." });

                            return Ok(operations);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error fetching operations.", Error = ex.Message });
            }
        }

    }
}
