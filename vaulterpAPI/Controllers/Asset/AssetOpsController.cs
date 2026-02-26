using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Asset;

namespace vaulterpAPI.Controllers.Asset
{
    [ApiController]
    [Route("api/asset/[controller]")]
    public class AssetOpsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public AssetOpsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ✅ Get all operations mapped to an asset
        [HttpGet("operations-by-asset")]
        public IActionResult GetOperationsByAsset([FromQuery] int assetId)
        {
            try
            {
                var ops = new List<dynamic>();
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    SELECT ao.id, ao.asset_id, ao.operation_id, o.operation_name, o.description
                    FROM asset.asset_operation ao
                    JOIN master.operation_master o ON ao.operation_id = o.operation_id
                    WHERE ao.is_active = TRUE AND ao.asset_id = @asset_id", conn);

                cmd.Parameters.AddWithValue("@asset_id", assetId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ops.Add(new
                    {
                        MappingId = (int)reader["id"],
                        AssetId = (int)reader["asset_id"],
                        OperationId = (int)reader["operation_id"],
                        OperationName = reader["operation_name"]?.ToString(),
                        Description = reader["description"]?.ToString()
                    });
                }

                return Ok(ops);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching asset operations", error = ex.Message });
            }
        }

        // ✅ Map asset to operations (replace existing)
        [HttpPost("map")]
        public IActionResult MapAssetToOperations([FromBody] AssetOperationMappingRequest request)
        {
            if (request.OperationIds == null || request.OperationIds.Count == 0)
                return BadRequest(new { message = "At least one operationId is required." });

            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                // Soft delete previous mappings
                var softDelete = new NpgsqlCommand(@"
                    UPDATE asset.asset_operation
                    SET is_active = FALSE, updated_on = NOW(), updated_by = @updated_by
                    WHERE asset_id = @asset_id AND is_active = TRUE", conn);
                softDelete.Parameters.AddWithValue("@asset_id", request.AssetId);
                softDelete.Parameters.AddWithValue("@updated_by", request.UpdatedBy);
                softDelete.ExecuteNonQuery();

                // Insert new mappings
                foreach (var opId in request.OperationIds)
                {
                    var insert = new NpgsqlCommand(@"
                        INSERT INTO asset.asset_operation 
                        (asset_id, operation_id, is_active, created_by, created_on, updated_on)
                        VALUES 
                        (@asset_id, @operation_id, TRUE, @created_by, NOW(), NOW())", conn);
                    insert.Parameters.AddWithValue("@asset_id", request.AssetId);
                    insert.Parameters.AddWithValue("@operation_id", opId);
                    insert.Parameters.AddWithValue("@created_by", request.UpdatedBy);
                    insert.ExecuteNonQuery();
                }

                return Ok(new { message = "Asset-operation mappings updated successfully", status = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error mapping asset to operations", error = ex.Message });
            }
        }

        // ✅ Soft-delete a single asset-operation mapping
        [HttpDelete("{id}")]
        public IActionResult DeleteAssetMapping(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    UPDATE asset.asset_operation
                    SET is_active = FALSE, updated_on = NOW()
                    WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                return Ok(new { message = "Mapping deleted (soft) successfully", status = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting mapping", error = ex.Message });
            }
        }

        // ✅ Get assets by operationId
        [HttpGet("assets-by-operation")]
        public IActionResult GetAssetsByOperation([FromQuery] int operationId)
        {
            try
            {
                var assets = new List<dynamic>();
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
            SELECT ao.id, ao.asset_id, am.asset_name, ao.operation_id, o.operation_name
            FROM asset.asset_operation ao
            JOIN asset.asset_master am ON ao.asset_id = am.asset_id
            JOIN master.operation_master o ON ao.operation_id = o.operation_id
            WHERE ao.is_active = TRUE AND ao.operation_id = @operation_id", conn);

                cmd.Parameters.AddWithValue("@operation_id", operationId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    assets.Add(new
                    {
                        MappingId = (int)reader["id"],
                        AssetId = (int)reader["asset_id"],
                        AssetName = reader["asset_name"]?.ToString(),
                        OperationId = (int)reader["operation_id"],
                        OperationName = reader["operation_name"]?.ToString()
                    });
                }

                return Ok(assets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching assets by operation", error = ex.Message });
            }
        }
    }
}
