using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using vaulterpAPI.Models.Asset;

namespace vaulterpAPI.Controllers.Asset
{
    [ApiController]
    [Route("api/asset/[controller]")]
    public class AssetSpareOpsController : ControllerBase
    {
        private readonly IConfiguration _config;
        public AssetSpareOpsController(IConfiguration config)
        {
            _config = config;
        }

        private string GetConnectionString() => _config.GetConnectionString("DefaultConnection");

        [HttpGet("all-assets/{officeId}")]
        public IActionResult GetAllAssetsByOffice(int officeId)
        {
            var assets = new List<object>();
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            string sql = @"
    SELECT am.asset_id,
       am.asset_name,
       COALESCE(m.id, 0) AS maintenance_id,
       m.status AS maintenance_status,
       m.out_from,
       m.sent_to,
       COALESCE(a.status, 'Pending') AS approval_status
FROM asset.asset_master am
LEFT JOIN LATERAL (
    SELECT id, status, out_from, sent_to
    FROM asset.asset_spare_maintenance
    WHERE asset_id = am.asset_id
    ORDER BY created_at DESC
    LIMIT 1
) m ON TRUE
LEFT JOIN LATERAL (
    SELECT status
    FROM asset.asset_spare_approval
    WHERE maintenance_id = m.id
    LIMIT 1
) a ON TRUE
WHERE am.office_id = @officeId
ORDER BY am.asset_id;
";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@officeId", officeId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                assets.Add(new
                {
                    Id = reader.GetInt32(0),
                    AssetName = reader.GetString(1),
                    MaintenanceId = reader.GetInt32(2),
                    MaintenanceStatus = reader.IsDBNull(3) ? "N/A" : reader.GetString(3),
                    OutFrom = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    SentTo = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    ApprovalStatus = reader.IsDBNull(6) ? "Pending" : reader.GetString(6)
                });
            }

            return Ok(assets);
        }


        // ------------------------------
        // 1️⃣ Add new spare to master
        // ------------------------------
        [HttpPost("add-spare")]
        public IActionResult AddSpare([FromBody] AssetSpareMaster spare)
        {
            try
            {
                using var con = new NpgsqlConnection(GetConnectionString());
                con.Open();

                var sql = @"
            INSERT INTO asset.asset_spare_master
            (spare_code, spare_name, part_number, category, specification, unit_of_measure,
             current_stock, reorder_level, reorder_quantity, location, linked_asset_id,
             vendor_name, purchase_rate, average_cost, lead_time_days, criticality,
             warranty_expiry, remarks, created_at, updated_at, is_new)
            VALUES
            (@code,@name,@part,@cat,@spec,@uom,@stock,@reorder_lvl,@reorder_qty,@loc,@linked_asset,
             @vendor,@purchase,@avg,@lead,@crit,@warranty,@remarks,NOW(),NOW(),TRUE)
            RETURNING spare_id";

                using var cmd = new NpgsqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@code", spare.SpareCode ?? "");
                cmd.Parameters.AddWithValue("@name", spare.SpareName ?? "");
                cmd.Parameters.AddWithValue("@part", spare.PartNumber ?? "");
                cmd.Parameters.AddWithValue("@cat", spare.Category ?? "");
                cmd.Parameters.AddWithValue("@spec", spare.Specification ?? "");
                cmd.Parameters.AddWithValue("@uom", spare.UnitOfMeasure ?? "");
                cmd.Parameters.AddWithValue("@stock", spare.CurrentStock);
                cmd.Parameters.AddWithValue("@reorder_lvl", spare.ReorderLevel);
                cmd.Parameters.AddWithValue("@reorder_qty", spare.ReorderQuantity);
                cmd.Parameters.AddWithValue("@loc", spare.Location ?? "");
                cmd.Parameters.AddWithValue("@linked_asset", (object)spare.LinkedAssetId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@vendor", spare.VendorName ?? "");
                cmd.Parameters.AddWithValue("@purchase", spare.PurchaseRate);
                cmd.Parameters.AddWithValue("@avg", spare.AverageCost);
                cmd.Parameters.AddWithValue("@lead", spare.LeadTimeDays);
                cmd.Parameters.AddWithValue("@crit", spare.Criticality ?? "");
                cmd.Parameters.AddWithValue("@warranty", (object)spare.WarrantyExpiry ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@remarks", spare.Remarks ?? "");

                int spareId = (int)cmd.ExecuteScalar();
                return Ok(new { message = "Spare added successfully", SpareId = spareId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        // ------------------------------
        // 2️⃣ Checkout / Checkin / Scrap
        // ------------------------------
        [HttpPost("log-action")]
        public IActionResult LogAction([FromForm] AssetSpareMaintenanceModel model, string actionType)
        {
            try
            {
                using var con = new NpgsqlConnection(GetConnectionString());
                con.Open();
                using var transaction = con.BeginTransaction();

                // Handle scrap value
                decimal costToStore = model.ScrapValue;
                if (actionType.ToLower() == "scrap") costToStore = -Math.Abs(model.ScrapValue);

                // Convert image files to byte[] (to store in DB)
                byte[]? imgOutBytes = null;
                byte[]? imgInBytes = null;

                if (!string.IsNullOrEmpty(model.ImageOutBase64))
                    imgOutBytes = Convert.FromBase64String(model.ImageOutBase64);

                if (!string.IsNullOrEmpty(model.ImageInBase64))
                    imgInBytes = Convert.FromBase64String(model.ImageInBase64);


                // Insert
                var sql = @"
        INSERT INTO asset.asset_spare_maintenance
        (spare_id, asset_id, issued_to, issued_by, issue_date, expected_return_date, actual_return_date,
         under_warranty, warranty_expiry, replacement_cost, scrap_value, return_condition, quantity,
         status, purpose, out_from, sent_to, image_out, image_in, remarks, created_at, updated_at)
        VALUES
        (@sid,@aid,@issuedTo,@issuedBy,@issue,@expected,@actual,
         @warranty,@wexpiry,@cost,@scrap,@cond,@qty,
         @status,@purpose,@outFrom,@sentTo,@imgOut,@imgIn,@remarks,NOW(),NOW())
        RETURNING id";

                using var cmd = new NpgsqlCommand(sql, con, transaction);
                cmd.Parameters.AddWithValue("@sid", model.SpareId);
                cmd.Parameters.AddWithValue("@aid", model.AssetId);
                cmd.Parameters.AddWithValue("@issuedTo", (object)model.IssuedTo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@issuedBy", (object)model.IssuedBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@issue", model.IssueDate);
                cmd.Parameters.AddWithValue("@expected", (object)model.ExpectedReturnDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@actual", (object)model.ActualReturnDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@warranty", model.UnderWarranty);
                cmd.Parameters.AddWithValue("@wexpiry", (object)model.WarrantyExpiry ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cost", model.ReplacementCost);
                cmd.Parameters.AddWithValue("@scrap", costToStore);
                cmd.Parameters.AddWithValue("@cond", model.ReturnCondition ?? "");
                cmd.Parameters.AddWithValue("@qty", model.Quantity);
                cmd.Parameters.AddWithValue("@status", model.Status ?? "Pending");
                cmd.Parameters.AddWithValue("@purpose", model.Purpose ?? "");
                cmd.Parameters.AddWithValue("@outFrom", model.OutFrom ?? "");
                cmd.Parameters.AddWithValue("@sentTo", model.SentTo ?? "");
                cmd.Parameters.AddWithValue("@imgOut", (object?)imgOutBytes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@imgIn", (object?)imgInBytes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@remarks", model.Remarks ?? "");

                int maintenanceId = (int)cmd.ExecuteScalar();

                // Insert approval
                if (actionType.ToLower() == "checkout" || actionType.ToLower() == "checkin" || actionType.ToLower() == "scrap")
                {
                    var approvalSql = @"
            INSERT INTO asset.asset_spare_approval
            (maintenance_id, action_type, approval_level, status, approved_at,approver_id)
            VALUES (@mid,@action,1,'Pending',NULL,@issuedTo)";

                    using var appCmd = new NpgsqlCommand(approvalSql, con, transaction);
                    appCmd.Parameters.AddWithValue("@mid", maintenanceId);
                    appCmd.Parameters.AddWithValue("@action", actionType);
                    appCmd.Parameters.AddWithValue("@issuedTo", (object)model.IssuedTo ?? DBNull.Value);
                    appCmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return Ok(new { message = $"{actionType} logged successfully, awaiting approval", MaintenanceId = maintenanceId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        // ------------------------------
        // 3️⃣ Replacement (scrap + new spare)
        // ------------------------------
        [HttpPost("replacement")]
        public IActionResult Replacement([FromForm] AssetSpareReplacementRequest request)
        {
            try
            {
                using var con = new NpgsqlConnection(GetConnectionString());
                con.Open();
                using var transaction = con.BeginTransaction();

                // 1️⃣ Scrap old spare
                var scrapModel = new AssetSpareMaintenanceModel
                {
                    SpareId = request.OldSpareId,
                    AssetId = request.AssetId,
                    Quantity = 1,
                    ScrapValue = request.ScrapValue,
                    Status = "Pending",
                    Purpose = "Replacement - Scrap old spare"
                };

                int scrapMaintenanceId = LogActionInternal(scrapModel, "Scrap", con, transaction);

                // 2️⃣ Determine new spare
                int newSpareId;
                if (request.UseExistingSpare && request.NewSpareId.HasValue)
                {
                    newSpareId = request.NewSpareId.Value;
                }
                else
                {
                    // Add new spare
                    var addSpareSql = @"
            INSERT INTO asset.asset_spare_master
            (spare_code, spare_name, part_number, category, specification, unit_of_measure,
             current_stock, reorder_level, reorder_quantity, location, linked_asset_id,
             vendor_name, purchase_rate, average_cost, lead_time_days, criticality,
             warranty_expiry, remarks, created_at, updated_at, is_new)
            VALUES
            (@code,@name,@part,@cat,@spec,@uom,@stock,@reorder_lvl,@reorder_qty,@loc,@linked_asset,
             @vendor,@purchase,@avg,@lead,@crit,@warranty,@remarks,NOW(),NOW(),TRUE)
            RETURNING spare_id";

                    using var cmd = new NpgsqlCommand(addSpareSql, con, transaction);
                    var newSpare = request.NewSpare;
                    cmd.Parameters.AddWithValue("@code", newSpare.SpareCode ?? "");
                    cmd.Parameters.AddWithValue("@name", newSpare.SpareName ?? "");
                    cmd.Parameters.AddWithValue("@part", newSpare.PartNumber ?? "");
                    cmd.Parameters.AddWithValue("@cat", newSpare.Category ?? "");
                    cmd.Parameters.AddWithValue("@spec", newSpare.Specification ?? "");
                    cmd.Parameters.AddWithValue("@uom", newSpare.UnitOfMeasure ?? "");
                    cmd.Parameters.AddWithValue("@stock", newSpare.CurrentStock);
                    cmd.Parameters.AddWithValue("@reorder_lvl", newSpare.ReorderLevel);
                    cmd.Parameters.AddWithValue("@reorder_qty", newSpare.ReorderQuantity);
                    cmd.Parameters.AddWithValue("@loc", newSpare.Location ?? "");
                    cmd.Parameters.AddWithValue("@linked_asset", (object)newSpare.LinkedAssetId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@vendor", newSpare.VendorName ?? "");
                    cmd.Parameters.AddWithValue("@purchase", newSpare.PurchaseRate);
                    cmd.Parameters.AddWithValue("@avg", newSpare.AverageCost);
                    cmd.Parameters.AddWithValue("@lead", newSpare.LeadTimeDays);
                    cmd.Parameters.AddWithValue("@crit", newSpare.Criticality ?? "");
                    cmd.Parameters.AddWithValue("@warranty", (object)newSpare.WarrantyExpiry ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@remarks", newSpare.Remarks ?? "");

                    newSpareId = (int)cmd.ExecuteScalar();
                }

                // 3️⃣ Insert into replacement table
                var replaceSql = @"
        INSERT INTO asset.asset_spare_replacement
        (maintenance_id, old_spare_id, new_spare_id, replaced_on, scrap_value, replacement_cost, remarks)
        VALUES (@mid,@old,@new,NOW(),@scrap,@cost,@remarks)";

                using var repCmd = new NpgsqlCommand(replaceSql, con, transaction);
                repCmd.Parameters.AddWithValue("@mid", scrapMaintenanceId);
                repCmd.Parameters.AddWithValue("@old", request.OldSpareId);
                repCmd.Parameters.AddWithValue("@new", newSpareId);
                repCmd.Parameters.AddWithValue("@scrap", request.ScrapValue);
                repCmd.Parameters.AddWithValue("@cost", request.ReplacementCost);
                repCmd.Parameters.AddWithValue("@remarks", request.Remarks ?? "");
                repCmd.ExecuteNonQuery();

                transaction.Commit();
                return Ok(new { message = "Replacement logged successfully", NewSpareId = newSpareId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ------------------------------
        // 4️⃣ Approve / Reject
        // ------------------------------
        [HttpPut("approve")]
        public IActionResult Approve(int maintenanceId, string status, int? approvedBy, string comments = null)
        {
            try
            {
                if (status != "Approved" && status != "Rejected")
                    return BadRequest("Status must be Approved or Rejected.");

                using var con = new NpgsqlConnection(GetConnectionString());
                con.Open();
                using var transaction = con.BeginTransaction();

                // 1️⃣ Update maintenance status
                var updateSql = @"
            UPDATE asset.asset_spare_maintenance
            SET status=@status, updated_at=NOW()
            WHERE id=@id";
                using var cmd = new NpgsqlCommand(updateSql, con, transaction);
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@id", maintenanceId);
                cmd.ExecuteNonQuery();

                // 2️⃣ Update approval table
                var updateApprovalSql = @"
            UPDATE asset.asset_spare_approval
            SET status=@status, approver_id=@approvedBy, comments=@comments, approved_at=NOW()
            WHERE maintenance_id=@id";
                using var appCmd = new NpgsqlCommand(updateApprovalSql, con, transaction);
                appCmd.Parameters.AddWithValue("@status", status);
                appCmd.Parameters.AddWithValue("@approvedBy", (object)approvedBy ?? DBNull.Value);
                appCmd.Parameters.AddWithValue("@comments", comments ?? "");
                appCmd.Parameters.AddWithValue("@id", maintenanceId);
                appCmd.ExecuteNonQuery();

                transaction.Commit();
                return Ok(new { message = $"Record {status}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        // ------------------------------
        // 5️⃣ Get report
        // ------------------------------
        [HttpGet("report")]
        public IActionResult GetReport([FromQuery] int? assetId)
        {
            try
            {
                var list = new List<dynamic>();
                using var con = new NpgsqlConnection(GetConnectionString());
                con.Open();

                var sql = @"
                    SELECT * FROM asset.asset_spare_maintenance
                    WHERE (@assetId IS NULL OR asset_id=@assetId)
                    ORDER BY issue_date DESC";

                using var cmd = new NpgsqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@assetId", (object)assetId ?? DBNull.Value);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new
                    {
                        Id = reader["id"],
                        AssetId = reader["asset_id"],
                        SpareId = reader["spare_id"],
                        IssuedTo = reader["issued_to"],
                        IssuedBy = reader["issued_by"],
                        IssueDate = reader["issue_date"],
                        ExpectedReturn = reader["expected_return_date"],
                        ActualReturn = reader["actual_return_date"],
                        Quantity = reader["quantity"],
                        Purpose = reader["purpose"]?.ToString(),
                        OutFrom = reader["out_from"]?.ToString(),
                        SentTo = reader["sent_to"]?.ToString(),
                        ReturnCondition = reader["return_condition"]?.ToString(),
                        Status = reader["status"]?.ToString(),
                        Remarks = reader["remarks"]?.ToString(),
                        ScrapValue = reader["scrap_value"] != DBNull.Value ? Math.Abs((decimal)reader["scrap_value"]) : 0,
                        ImageOutBase64 = reader["image_out"] != DBNull.Value ? Convert.ToBase64String((byte[])reader["image_out"]) : null,
                        ImageInBase64 = reader["image_in"] != DBNull.Value ? Convert.ToBase64String((byte[])reader["image_in"]) : null
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching report", error = ex.Message });
            }
        }

        [HttpPost("checkin-full")]
        public IActionResult CheckInWithReplacementAndApproval([FromBody] CheckInRequest request)
        {
            if (request.Maintenance == null)
                return BadRequest("Maintenance data is required.");

            using var con = new NpgsqlConnection(GetConnectionString());
            con.Open();
            using var transaction = con.BeginTransaction();

            // 1️⃣ Log check-in
            int maintenanceId = LogActionInternal(request.Maintenance, "CheckIn", con, transaction);

            // 2️⃣ Handle replacement if needed
            if (request.ReplacementRequired && request.Replacement != null)
            {
                HandleReplacementInternal(request.Replacement, maintenanceId, con, transaction);
            }

            transaction.Commit();
            return Ok(new { message = "Check-In completed with replacement & approval", MaintenanceId = maintenanceId });
        }


        // ------------------------------
        // Internal replacement handler
        // ------------------------------
        private void HandleReplacementInternal(
    AssetSpareReplacementRequest replacement,
    int maintenanceId,
    NpgsqlConnection con,
    NpgsqlTransaction transaction)
        {
            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            // 1. Decide which spare is new
            int newSpareId;
            if (replacement.UseExistingSpare && replacement.NewSpareId.HasValue)
            {
                // Use an already existing spare
                newSpareId = replacement.NewSpareId.Value;
            }
            else
            {
                // Insert brand new spare into asset.asset_spare_master
                newSpareId = InsertNewSpare(replacement.NewSpare, con, transaction);
            }

            // 2. Link old spare → new spare under the SAME maintenance record (no new scrap maintenance)
            using (var cmd = new NpgsqlCommand(@"
        INSERT INTO asset.asset_spare_replacement
        (maintenance_id, old_spare_id, new_spare_id, replaced_on, scrap_value, replacement_cost, remarks)
        VALUES (@mid, @old, @new, NOW(), @scrap, @cost, @remarks)", con, transaction))
            {
                cmd.Parameters.AddWithValue("@mid", maintenanceId);
                cmd.Parameters.AddWithValue("@old", replacement.OldSpareId);
                cmd.Parameters.AddWithValue("@new", newSpareId);
                cmd.Parameters.AddWithValue("@scrap", replacement.ScrapValue);
                cmd.Parameters.AddWithValue("@cost", replacement.ReplacementCost);
                cmd.Parameters.AddWithValue("@remarks", replacement.Remarks ?? "");
                cmd.ExecuteNonQuery();
            }

            // ⚠️ Note: We’re NOT creating a new maintenance record or approval for "Scrap".
            // The replacement is tied to the original CheckIn maintenance/approval.
        }

        private int InsertNewSpare(AssetSpareMaster newSpare, NpgsqlConnection con, NpgsqlTransaction transaction)
        {
            if (newSpare == null)
                throw new ArgumentNullException(nameof(newSpare));

            using (var cmd = new NpgsqlCommand(@"
        INSERT INTO asset.asset_spare_master
        (spare_code, spare_name, part_number, category, specification, unit_of_measure,
         current_stock, reorder_level, reorder_quantity, location, linked_asset_id, vendor_name,
         purchase_rate, average_cost, lead_time_days, criticality, warranty_expiry, remarks,
         created_at, updated_at, is_new)
        VALUES (@code, @name, @part, @cat, @spec, @uom,
                @stock, @rl, @rq, @loc, @asset, @vendor,
                @prate, @avg, @lead, @crit, @wexp, @remarks,
                NOW(), NOW(), TRUE)
        RETURNING spare_id;", con, transaction))
            {
                cmd.Parameters.AddWithValue("@code", newSpare.SpareCode ?? "");
                cmd.Parameters.AddWithValue("@name", newSpare.SpareName ?? "");
                cmd.Parameters.AddWithValue("@part", newSpare.PartNumber ?? "");
                cmd.Parameters.AddWithValue("@cat", newSpare.Category ?? "");
                cmd.Parameters.AddWithValue("@spec", newSpare.Specification ?? "");
                cmd.Parameters.AddWithValue("@uom", newSpare.UnitOfMeasure ?? "");
                cmd.Parameters.AddWithValue("@stock", newSpare.CurrentStock);
                cmd.Parameters.AddWithValue("@rl", newSpare.ReorderLevel);
                cmd.Parameters.AddWithValue("@rq", newSpare.ReorderQuantity);
                cmd.Parameters.AddWithValue("@loc", newSpare.Location ?? "");
                cmd.Parameters.AddWithValue("@asset", newSpare.LinkedAssetId);
                cmd.Parameters.AddWithValue("@vendor", newSpare.VendorName ?? "");
                cmd.Parameters.AddWithValue("@prate", newSpare.PurchaseRate);
                cmd.Parameters.AddWithValue("@avg", newSpare.AverageCost);
                cmd.Parameters.AddWithValue("@lead", newSpare.LeadTimeDays);
                cmd.Parameters.AddWithValue("@crit", newSpare.Criticality ?? "");
                cmd.Parameters.AddWithValue("@wexp", newSpare.WarrantyExpiry ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@remarks", newSpare.Remarks ?? "");

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private int LogActionInternal(AssetSpareMaintenanceModel model, string actionType, NpgsqlConnection con, NpgsqlTransaction transaction)
        {
            decimal costToStore = model.ScrapValue;
            if (actionType.ToLower() == "scrap") costToStore = -Math.Abs(model.ScrapValue);
            byte[]? imgOutBytes = null;
            byte[]? imgInBytes = null;

            if (!string.IsNullOrEmpty(model.ImageOutBase64))
                imgOutBytes = Convert.FromBase64String(model.ImageOutBase64);

            if (!string.IsNullOrEmpty(model.ImageInBase64))
                imgInBytes = Convert.FromBase64String(model.ImageInBase64);


            var sql = @"
        INSERT INTO asset.asset_spare_maintenance
        (spare_id, asset_id, issued_to, issued_by, issue_date, expected_return_date, actual_return_date,
         under_warranty, warranty_expiry, replacement_cost, scrap_value, return_condition, quantity,
         status, purpose, out_from, sent_to, image_out, image_in, remarks, created_at, updated_at)
        VALUES
        (@sid,@aid,@issuedTo,@issuedBy,@issue,@expected,@actual,
         @warranty,@wexpiry,@cost,@scrap,@cond,@qty,
         @status,@purpose,@outFrom,@sentTo,@imgOut,@imgIn,@remarks,NOW(),NOW())
        RETURNING id";

            using var cmd = new NpgsqlCommand(sql, con, transaction);
            cmd.Parameters.AddWithValue("@sid", model.SpareId);
            cmd.Parameters.AddWithValue("@aid", model.AssetId);
            cmd.Parameters.AddWithValue("@issuedTo", (object)model.IssuedTo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@issuedBy", (object)model.IssuedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@issue", model.IssueDate);
            cmd.Parameters.AddWithValue("@expected", (object)model.ExpectedReturnDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@actual", (object)model.ActualReturnDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@warranty", model.UnderWarranty);
            cmd.Parameters.AddWithValue("@wexpiry", (object)model.WarrantyExpiry ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cost", model.ReplacementCost);
            cmd.Parameters.AddWithValue("@scrap", costToStore);
            cmd.Parameters.AddWithValue("@cond", model.ReturnCondition ?? "");
            cmd.Parameters.AddWithValue("@qty", model.Quantity);
            cmd.Parameters.AddWithValue("@status", model.Status ?? "Pending");
            cmd.Parameters.AddWithValue("@purpose", model.Purpose ?? "");
            cmd.Parameters.AddWithValue("@outFrom", model.OutFrom ?? "");
            cmd.Parameters.AddWithValue("@sentTo", model.SentTo ?? "");
            cmd.Parameters.AddWithValue("@imgOut", (object?)imgOutBytes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@imgIn", (object?)imgInBytes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@remarks", model.Remarks ?? "");

            int maintenanceId = (int)cmd.ExecuteScalar();

            // Create approval record if required
            if (actionType.ToLower() == "checkout" || actionType.ToLower() == "checkin" || actionType.ToLower() == "scrap")
            {
                var approvalSql = @"
            INSERT INTO asset.asset_spare_approval
            (maintenance_id, action_type, approval_level, status, approved_at)
            VALUES (@mid,@action,1,'Pending',NULL)";
                using var appCmd = new NpgsqlCommand(approvalSql, con, transaction);
                appCmd.Parameters.AddWithValue("@mid", maintenanceId);
                appCmd.Parameters.AddWithValue("@action", actionType);
                appCmd.ExecuteNonQuery();
            }

            return maintenanceId;
        }
    }
}
