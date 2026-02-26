using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.work_order;

namespace vaulterpAPI.Controllers.work_order
{
    [ApiController]
    [Route("api/work_order/[controller]")]
    public class ProductMasterController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ProductMasterController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");



        // ✅ Get by ID
        [HttpGet("byid{id}")]
        public IActionResult GetById(int id)
        {
            ProductMaster? data = null;
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT * FROM work_order.product_master WHERE id = @id AND is_active = 0", conn);
            cmd.Parameters.AddWithValue("id", id);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                data = new ProductMaster
                {
                    id = Convert.ToInt32(reader["id"]),
                    product_name = reader["product_name"].ToString(),
                    description = reader["description"]?.ToString(),
                    rate = reader["rate"] as int?,
                    unit = reader["unit"]?.ToString(),
                    is_active = reader["is_active"] as int?,
                    office_id = reader["office_id"] as int?,
                    createdon = reader["createdon"] as DateTime?,
                    createdby = reader["createdby"] as int?,
                    updatedon = reader["updatedon"] as DateTime?,
                    updatedby = reader["updatedby"] as int?
                };
            }
            return data == null ? NotFound() : Ok(data);
        }

        // ✅ Get by Office ID
        [HttpGet("Office/{officeId}")]
        public IActionResult GetByOfficeId(int officeId)
        {
            List<ProductMaster> list = new List<ProductMaster>();
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT * FROM work_order.product_master WHERE office_id = @officeId AND is_active = 0", conn);
            cmd.Parameters.AddWithValue("officeId", officeId);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new ProductMaster
                {
                    id = Convert.ToInt32(reader["id"]),
                    product_name = reader["product_name"].ToString(),
                    description = reader["description"]?.ToString(),
                    rate = reader["rate"] as int?,
                    unit = reader["unit"]?.ToString(),
                    is_active = reader["is_active"] as int?,
                    office_id = reader["office_id"] as int?,
                    createdon = reader["createdon"] as DateTime?,
                    createdby = reader["createdby"] as int?,
                    updatedon = reader["updatedon"] as DateTime?,
                    updatedby = reader["updatedby"] as int?
                });
            }
            return Ok(list);
        }

        // ✅ POST - Create
        [HttpPost]
        public IActionResult Create([FromBody] ProductMaster model)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            string query = @"
                INSERT INTO work_order.product_master
                (product_name, description, rate, unit, is_active, office_id, createdon, createdby)
                VALUES
                (@product_name, @description, @rate, @unit, @is_active, @office_id, @createdon, @createdby)";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("product_name", model.product_name);
            cmd.Parameters.AddWithValue("description", model.description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("rate", model.rate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("unit", model.unit ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("is_active", model.is_active ?? 0);
            cmd.Parameters.AddWithValue("office_id", model.office_id ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("createdon", model.createdon ?? DateTime.UtcNow);
            cmd.Parameters.AddWithValue("createdby", model.createdby ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
            return Ok(new { message = "Product created successfully" });
        }

        // ✅ PUT - Update
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] ProductMaster model)
        {
            if (id != model.id)
                return BadRequest("ID mismatch.");

            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            string query = @"
                UPDATE work_order.product_master SET
                    product_name = @product_name,
                    description = @description,
                    rate = @rate,
                    unit = @unit,
                    is_active = @is_active,
                    office_id = @office_id,
                    updatedon = @updatedon,
                    updatedby = @updatedby
                WHERE id = @id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("product_name", model.product_name);
            cmd.Parameters.AddWithValue("description", model.description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("rate", model.rate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("unit", model.unit ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("is_active", model.is_active ?? 0);
            cmd.Parameters.AddWithValue("office_id", model.office_id ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("updatedon", model.updatedon ?? DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedby", model.updatedby ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
            return Ok(new { message = "Product updated successfully" });
        }

        // ✅ DELETE - Soft Delete
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            string query = "UPDATE work_order.product_master SET is_active = 1 WHERE id = @id";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("id", id);

            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected == 0 ? NotFound() : Ok(new { message = "Product soft-deleted successfully" });
        }
    }
}
