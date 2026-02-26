using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Asset;

namespace vaulterpAPI.Controllers.Asset
{
    [ApiController]
    [Route("api/asset/[controller]")]
    public class AssetSpareController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AssetSpareController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ✅ Get all spares
        [HttpGet]
        public IActionResult GetAllSpares()
        {
            try
            {
                var spares = new List<dynamic>();
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    SELECT spare_id, spare_code, spare_name, part_number, category,
                           specification, unit_of_measure, current_stock,
                           reorder_level, reorder_quantity, location, linked_asset_id,
                           vendor_name, purchase_rate, average_cost, lead_time_days,
                           criticality, warranty_expiry, remarks, created_at, updated_at
                    FROM asset.asset_spare_master
                    ORDER BY spare_id DESC", conn);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    spares.Add(new
                    {
                        SpareId = (int)reader["spare_id"],
                        SpareCode = reader["spare_code"].ToString(),
                        SpareName = reader["spare_name"].ToString(),
                        PartNumber = reader["part_number"]?.ToString(),
                        Category = reader["category"]?.ToString(),
                        Specification = reader["specification"]?.ToString(),
                        UnitOfMeasure = reader["unit_of_measure"].ToString(),
                        CurrentStock = reader["current_stock"] is DBNull ? 0 : (int)reader["current_stock"],
                        ReorderLevel = reader["reorder_level"] is DBNull ? 0 : (int)reader["reorder_level"],
                        ReorderQuantity = reader["reorder_quantity"] is DBNull ? 0 : (int)reader["reorder_quantity"],
                        Location = reader["location"]?.ToString(),
                        LinkedAssetId = reader["linked_asset_id"] is DBNull ? null : (int?)reader["linked_asset_id"],
                        VendorName = reader["vendor_name"]?.ToString(),
                        PurchaseRate = reader["purchase_rate"] is DBNull ? 0 : (decimal)reader["purchase_rate"],
                        AverageCost = reader["average_cost"] is DBNull ? 0 : (decimal)reader["average_cost"],
                        LeadTimeDays = reader["lead_time_days"] is DBNull ? 0 : (int)reader["lead_time_days"],
                        Criticality = reader["criticality"]?.ToString(),
                        WarrantyExpiry = reader["warranty_expiry"] is DBNull ? null : (DateTime?)reader["warranty_expiry"],
                        Remarks = reader["remarks"]?.ToString(),
                        CreatedAt = (DateTime)reader["created_at"],
                        UpdatedAt = (DateTime)reader["updated_at"]
                    });
                }

                return Ok(spares);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching spares", error = ex.Message });
            }
        }

        // ✅ Get spare by id
        [HttpGet("{id}")]
        public IActionResult GetSpareById(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    SELECT * FROM asset.asset_spare_master
                    WHERE spare_id = @id", conn);

                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return Ok(new
                    {
                        SpareId = (int)reader["spare_id"],
                        SpareCode = reader["spare_code"].ToString(),
                        SpareName = reader["spare_name"].ToString(),
                        PartNumber = reader["part_number"]?.ToString(),
                        Category = reader["category"]?.ToString(),
                        Specification = reader["specification"]?.ToString(),
                        UnitOfMeasure = reader["unit_of_measure"].ToString(),
                        CurrentStock = reader["current_stock"] is DBNull ? 0 : (int)reader["current_stock"],
                        ReorderLevel = reader["reorder_level"] is DBNull ? 0 : (int)reader["reorder_level"],
                        ReorderQuantity = reader["reorder_quantity"] is DBNull ? 0 : (int)reader["reorder_quantity"],
                        Location = reader["location"]?.ToString(),
                        LinkedAssetId = reader["linked_asset_id"] is DBNull ? null : (int?)reader["linked_asset_id"],
                        VendorName = reader["vendor_name"]?.ToString(),
                        PurchaseRate = reader["purchase_rate"] is DBNull ? 0 : (decimal)reader["purchase_rate"],
                        AverageCost = reader["average_cost"] is DBNull ? 0 : (decimal)reader["average_cost"],
                        LeadTimeDays = reader["lead_time_days"] is DBNull ? 0 : (int)reader["lead_time_days"],
                        Criticality = reader["criticality"]?.ToString(),
                        WarrantyExpiry = reader["warranty_expiry"] is DBNull ? null : (DateTime?)reader["warranty_expiry"],
                        Remarks = reader["remarks"]?.ToString(),
                        CreatedAt = (DateTime)reader["created_at"],
                        UpdatedAt = (DateTime)reader["updated_at"]
                    });
                }

                return NotFound(new { message = "Spare not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching spare", error = ex.Message });
            }
        }

        // ✅ Create new spare
        [HttpPost]
        public IActionResult CreateSpare([FromBody] AssetSpareRequest request)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    INSERT INTO asset.asset_spare_master
                    (spare_code, spare_name, part_number, category, specification,
                     unit_of_measure, current_stock, reorder_level, reorder_quantity,
                     location, linked_asset_id, vendor_name, purchase_rate,
                     average_cost, lead_time_days, criticality, warranty_expiry,
                     remarks, created_at, updated_at)
                    VALUES
                    (@spare_code, @spare_name, @part_number, @category, @specification,
                     @unit_of_measure, @current_stock, @reorder_level, @reorder_quantity,
                     @location, @linked_asset_id, @vendor_name, @purchase_rate,
                     @average_cost, @lead_time_days, @criticality, @warranty_expiry,
                     @remarks, NOW(), NOW())
                    RETURNING spare_id", conn);

                cmd.Parameters.AddWithValue("@spare_code", request.SpareCode ?? throw new ArgumentNullException(nameof(request.SpareCode)));
                cmd.Parameters.AddWithValue("@spare_name", request.SpareName ?? throw new ArgumentNullException(nameof(request.SpareName)));
                cmd.Parameters.AddWithValue("@part_number", request.PartNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@category", request.Category ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@specification", request.Specification ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@unit_of_measure", request.UnitOfMeasure ?? throw new ArgumentNullException(nameof(request.UnitOfMeasure)));
                cmd.Parameters.AddWithValue("@current_stock", request.CurrentStock ?? 0);
                cmd.Parameters.AddWithValue("@reorder_level", request.ReorderLevel ?? 0);
                cmd.Parameters.AddWithValue("@reorder_quantity", request.ReorderQuantity ?? 0);
                cmd.Parameters.AddWithValue("@location", request.Location ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@linked_asset_id", request.LinkedAssetId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@vendor_name", request.VendorName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@purchase_rate", request.PurchaseRate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@average_cost", request.AverageCost ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@lead_time_days", request.LeadTimeDays ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@criticality", request.Criticality ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@warranty_expiry", request.WarrantyExpiry ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@remarks", request.Remarks ?? (object)DBNull.Value);

                var newId = (int)cmd.ExecuteScalar();

                return Ok(new { message = "Spare created successfully", id = newId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating spare", error = ex.Message });
            }
        }

        // ✅ Update spare
        [HttpPut("{id}")]
        public IActionResult UpdateSpare(int id, [FromBody] AssetSpareRequest request)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    UPDATE asset.asset_spare_master
                    SET spare_code = @spare_code,
                        spare_name = @spare_name,
                        part_number = @part_number,
                        category = @category,
                        specification = @specification,
                        unit_of_measure = @unit_of_measure,
                        current_stock = @current_stock,
                        reorder_level = @reorder_level,
                        reorder_quantity = @reorder_quantity,
                        location = @location,
                        linked_asset_id = @linked_asset_id,
                        vendor_name = @vendor_name,
                        purchase_rate = @purchase_rate,
                        average_cost = @average_cost,
                        lead_time_days = @lead_time_days,
                        criticality = @criticality,
                        warranty_expiry = @warranty_expiry,
                        remarks = @remarks,
                        updated_at = NOW()
                    WHERE spare_id = @id", conn);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@spare_code", request.SpareCode ?? throw new ArgumentNullException(nameof(request.SpareCode)));
                cmd.Parameters.AddWithValue("@spare_name", request.SpareName ?? throw new ArgumentNullException(nameof(request.SpareName)));
                cmd.Parameters.AddWithValue("@part_number", request.PartNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@category", request.Category ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@specification", request.Specification ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@unit_of_measure", request.UnitOfMeasure ?? throw new ArgumentNullException(nameof(request.UnitOfMeasure)));
                cmd.Parameters.AddWithValue("@current_stock", request.CurrentStock ?? 0);
                cmd.Parameters.AddWithValue("@reorder_level", request.ReorderLevel ?? 0);
                cmd.Parameters.AddWithValue("@reorder_quantity", request.ReorderQuantity ?? 0);
                cmd.Parameters.AddWithValue("@location", request.Location ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@linked_asset_id", request.LinkedAssetId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@vendor_name", request.VendorName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@purchase_rate", request.PurchaseRate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@average_cost", request.AverageCost ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@lead_time_days", request.LeadTimeDays ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@criticality", request.Criticality ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@warranty_expiry", request.WarrantyExpiry ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@remarks", request.Remarks ?? (object)DBNull.Value);

                var rows = cmd.ExecuteNonQuery();

                return Ok(new { message = "Spare updated successfully", updatedRows = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating spare", error = ex.Message });
            }
        }

        // ✅ Delete spare
        [HttpDelete("{id}")]
        public IActionResult DeleteSpare(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    DELETE FROM asset.asset_spare_master
                    WHERE spare_id = @id", conn);

                cmd.Parameters.AddWithValue("@id", id);
                var rows = cmd.ExecuteNonQuery();

                return Ok(new { message = "Spare deleted successfully", deletedRows = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting spare", error = ex.Message });
            }
        }

        // ✅ Get spares by linked_asset_id
        [HttpGet("asset/{assetId}")]
        public IActionResult GetSparesByAssetId(int assetId)
        {
            try
            {
                var spares = new List<dynamic>();
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    SELECT spare_id, spare_code, spare_name, part_number, category,
                           specification, unit_of_measure, current_stock,
                           reorder_level, reorder_quantity, location, linked_asset_id,
                           vendor_name, purchase_rate, average_cost, lead_time_days,
                           criticality, warranty_expiry, remarks, created_at, updated_at
                    FROM asset.asset_spare_master
                    WHERE linked_asset_id = @assetId
                    ORDER BY spare_id DESC", conn);

                cmd.Parameters.AddWithValue("@assetId", assetId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    spares.Add(new
                    {
                        SpareId = (int)reader["spare_id"],
                        SpareCode = reader["spare_code"].ToString(),
                        SpareName = reader["spare_name"].ToString(),
                        PartNumber = reader["part_number"]?.ToString(),
                        Category = reader["category"]?.ToString(),
                        Specification = reader["specification"]?.ToString(),
                        UnitOfMeasure = reader["unit_of_measure"].ToString(),
                        CurrentStock = reader["current_stock"] is DBNull ? 0 : (int)reader["current_stock"],
                        ReorderLevel = reader["reorder_level"] is DBNull ? 0 : (int)reader["reorder_level"],
                        ReorderQuantity = reader["reorder_quantity"] is DBNull ? 0 : (int)reader["reorder_quantity"],
                        Location = reader["location"]?.ToString(),
                        LinkedAssetId = reader["linked_asset_id"] is DBNull ? null : (int?)reader["linked_asset_id"],
                        VendorName = reader["vendor_name"]?.ToString(),
                        PurchaseRate = reader["purchase_rate"] is DBNull ? 0 : (decimal)reader["purchase_rate"],
                        AverageCost = reader["average_cost"] is DBNull ? 0 : (decimal)reader["average_cost"],
                        LeadTimeDays = reader["lead_time_days"] is DBNull ? 0 : (int)reader["lead_time_days"],
                        Criticality = reader["criticality"]?.ToString(),
                        WarrantyExpiry = reader["warranty_expiry"] is DBNull ? null : (DateTime?)reader["warranty_expiry"],
                        Remarks = reader["remarks"]?.ToString(),
                        CreatedAt = (DateTime)reader["created_at"],
                        UpdatedAt = (DateTime)reader["updated_at"]
                    });
                }

                if (spares.Count == 0)
                    return NotFound(new { message = "No spares found for this asset" });

                return Ok(spares);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching spares by assetId", error = ex.Message });
            }
        }
        // ✅ Get spare(s) by spare_name (supports partial match)
        [HttpGet("name/{spareName}")]
        public IActionResult GetSparesByName(string spareName)
        {
            try
            {
                var spares = new List<dynamic>();
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
            SELECT spare_id, spare_code, spare_name, part_number, category,
                   specification, unit_of_measure, current_stock,
                   reorder_level, reorder_quantity, location, linked_asset_id,
                   vendor_name, purchase_rate, average_cost, lead_time_days,
                   criticality, warranty_expiry, remarks, created_at, updated_at
            FROM asset.asset_spare_master
            WHERE LOWER(spare_name) LIKE LOWER(@spareName)
            ORDER BY spare_id DESC", conn);

                // Use ILIKE for case-insensitive search in Postgres
                cmd.Parameters.AddWithValue("@spareName", $"%{spareName}%");

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    spares.Add(new
                    {
                        SpareId = (int)reader["spare_id"],
                        SpareCode = reader["spare_code"].ToString(),
                        SpareName = reader["spare_name"].ToString(),
                        PartNumber = reader["part_number"]?.ToString(),
                        Category = reader["category"]?.ToString(),
                        Specification = reader["specification"]?.ToString(),
                        UnitOfMeasure = reader["unit_of_measure"].ToString(),
                        CurrentStock = reader["current_stock"] is DBNull ? 0 : (int)reader["current_stock"],
                        ReorderLevel = reader["reorder_level"] is DBNull ? 0 : (int)reader["reorder_level"],
                        ReorderQuantity = reader["reorder_quantity"] is DBNull ? 0 : (int)reader["reorder_quantity"],
                        Location = reader["location"]?.ToString(),
                        LinkedAssetId = reader["linked_asset_id"] is DBNull ? null : (int?)reader["linked_asset_id"],
                        VendorName = reader["vendor_name"]?.ToString(),
                        PurchaseRate = reader["purchase_rate"] is DBNull ? 0 : (decimal)reader["purchase_rate"],
                        AverageCost = reader["average_cost"] is DBNull ? 0 : (decimal)reader["average_cost"],
                        LeadTimeDays = reader["lead_time_days"] is DBNull ? 0 : (int)reader["lead_time_days"],
                        Criticality = reader["criticality"]?.ToString(),
                        WarrantyExpiry = reader["warranty_expiry"] is DBNull ? null : (DateTime?)reader["warranty_expiry"],
                        Remarks = reader["remarks"]?.ToString(),
                        CreatedAt = (DateTime)reader["created_at"],
                        UpdatedAt = (DateTime)reader["updated_at"]
                    });
                }

                if (spares.Count == 0)
                    return NotFound(new { message = "No spares found with the given name" });

                return Ok(spares);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching spares by name", error = ex.Message });
            }
        }
    }
}