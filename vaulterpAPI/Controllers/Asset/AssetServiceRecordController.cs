using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Asset;

namespace vaulterpAPI.Controllers.AssetServiceRecord
{
    [ApiController]
    [Route("api/assetservice/[controller]")]
    public class AssetServiceRecordController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public AssetServiceRecordController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");
        [HttpGet("GetServiceHistory")]
        public async Task<IActionResult> GetServiceHistory(int assetId)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync();

                var query = @"
            SELECT id, assetid, servicedate, nextservicedate, image, remark,
                   servicedoc, servicecost, servicedby, approvedby,
                   duration, is_approved, is_rejected, rejection_remark,days
            FROM asset.asset_service_records
            WHERE assetid = @assetid
            ORDER BY servicedate DESC;";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@assetid", assetId);

                using var reader = await cmd.ExecuteReaderAsync();
                var serviceRecords = new List<AssetServiceRecordDto>();

                while (await reader.ReadAsync())
                {
                    serviceRecords.Add(new AssetServiceRecordDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        AssetId = reader.GetInt32(reader.GetOrdinal("assetid")),
                        ServiceDate = reader.IsDBNull(reader.GetOrdinal("servicedate")) ? null :
                                      reader.GetDateTime(reader.GetOrdinal("servicedate")).ToString("yyyy-MM-dd"),
                        NextServiceDate = reader.IsDBNull(reader.GetOrdinal("nextservicedate")) ? null :
                                          reader.GetDateTime(reader.GetOrdinal("nextservicedate")).ToString("yyyy-MM-dd"),
                        Image = reader.IsDBNull(reader.GetOrdinal("image")) ? null :
                                Convert.ToBase64String((byte[])reader["image"]),
                        Remark = reader.IsDBNull(reader.GetOrdinal("remark")) ? null : reader.GetString(reader.GetOrdinal("remark")),
                        ServiceDoc = reader.IsDBNull(reader.GetOrdinal("servicedoc")) ? null :
                                     Convert.ToBase64String((byte[])reader["servicedoc"]),
                        ServiceCost = reader.IsDBNull(reader.GetOrdinal("servicecost")) ? 0 : reader.GetInt32(reader.GetOrdinal("servicecost")),
                        ServicedBy = reader.IsDBNull(reader.GetOrdinal("servicedby")) ? null : reader.GetString(reader.GetOrdinal("servicedby")),
                        ApprovedBy = reader.IsDBNull(reader.GetOrdinal("approvedby")) ? null : reader.GetString(reader.GetOrdinal("approvedby")),
                        Duration = reader.IsDBNull(reader.GetOrdinal("duration"))
    ? (TimeSpan?)null
    : reader.GetTimeSpan(reader.GetOrdinal("duration")),
                        Days = reader.IsDBNull(reader.GetOrdinal("days")) ? 0 : reader.GetInt32(reader.GetOrdinal("days")),
                        IsApproved = reader.IsDBNull(reader.GetOrdinal("is_approved")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("is_approved")),
                        IsRejected = reader.IsDBNull(reader.GetOrdinal("is_rejected")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("is_rejected")),
                        RejectionRemark = reader.IsDBNull(reader.GetOrdinal("rejection_remark")) ? null : reader.GetString(reader.GetOrdinal("rejection_remark"))
                    });
                }

                return Ok(serviceRecords);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving service history", Error = ex.Message });
            }
        }

        [HttpPost("SaveServiceRecord")]
        public async Task<IActionResult> SaveServiceRecord([FromForm] AssetServiceRecordForm record)
        {
            if (record.AssetId <= 0 || record.ServiceDate == null || record.NextServiceDate == null)
                return BadRequest("Invalid input data.");

            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync();

                await using var transaction = await conn.BeginTransactionAsync();

                using var cmd = new NpgsqlCommand(@"
            INSERT INTO asset.asset_service_records
                (assetid, servicedate, nextservicedate, image, remark,
                 servicedoc, servicecost, servicedby, approvedby,
                 duration,days, is_approved, is_rejected, rejection_remark,
                 createdby, createdon)
            VALUES
                (@assetid, @servicedate, @nextservicedate, @image, @remark,
                 @servicedoc, @servicecost, @servicedby, @approvedby,
                 @duration,@days, @isApproved, @isRejected, @rejectionRemark,
                 @createdby, CURRENT_TIMESTAMP);

            UPDATE asset.asset_master
            SET last_service_date = @servicedate,
                next_service_date = @nextservicedate
            WHERE asset_id = @assetid;", conn, transaction);

                cmd.Parameters.AddWithValue("@assetid", record.AssetId);
                cmd.Parameters.AddWithValue("@servicedate", record.ServiceDate.Value);
                cmd.Parameters.AddWithValue("@nextservicedate", record.NextServiceDate.Value);

                // Image
                if (record.Image != null)
                {
                    using var ms = new MemoryStream();
                    await record.Image.CopyToAsync(ms);
                    cmd.Parameters.AddWithValue("@image", ms.ToArray());
                }
                else cmd.Parameters.AddWithValue("@image", DBNull.Value);

                // Service Doc
                if (record.ServiceDoc != null)
                {
                    using var ms = new MemoryStream();
                    await record.ServiceDoc.CopyToAsync(ms);
                    cmd.Parameters.AddWithValue("@servicedoc", ms.ToArray());
                }
                else cmd.Parameters.AddWithValue("@servicedoc", DBNull.Value);

                cmd.Parameters.AddWithValue("@remark", string.IsNullOrWhiteSpace(record.Remark) ? DBNull.Value : record.Remark);
                cmd.Parameters.AddWithValue("@servicecost", record.ServiceCost);
                cmd.Parameters.AddWithValue("@servicedby", string.IsNullOrWhiteSpace(record.ServicedBy) ? DBNull.Value : record.ServicedBy);
                cmd.Parameters.AddWithValue("@approvedby", string.IsNullOrWhiteSpace(record.ApprovedBy) ? DBNull.Value : record.ApprovedBy);
                cmd.Parameters.AddWithValue("@duration", record.Duration);
                cmd.Parameters.AddWithValue("@days", record.Days);
                cmd.Parameters.AddWithValue("@isApproved", record.IsApproved.HasValue ? (object)record.IsApproved.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@isRejected", record.IsRejected.HasValue ? (object)record.IsRejected.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@rejectionRemark", string.IsNullOrWhiteSpace(record.RejectionRemark) ? DBNull.Value : record.RejectionRemark);
                cmd.Parameters.AddWithValue("@createdby", string.IsNullOrWhiteSpace(record.CreatedBy) ? DBNull.Value : record.CreatedBy);

                var rows = await cmd.ExecuteNonQueryAsync();
                await transaction.CommitAsync();

                return rows > 0 ? Ok("Service Record added successfully.") :
                                  StatusCode(500, "Failed to add Service Record.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error saving service record", Error = ex.Message });
            }
        }
        [HttpPost("ApproveOrReject")]
        public async Task<IActionResult> ApproveOrReject([FromBody] ApproveRejectRequest request)
        {
            if (request.RecordId <= 0 || string.IsNullOrWhiteSpace(request.ApprovedBy))
                return BadRequest("Invalid input data.");

            if (request.IsApproved && request.IsRejected)
                return BadRequest("Cannot approve and reject at the same time.");

            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync();

                var query = @"
            UPDATE asset.asset_service_records
            SET is_approved = @isApproved,
                is_rejected = @isRejected,
                rejection_remark = @rejectionRemark,
                approvedby = @approvedBy
            WHERE id = @recordId;";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@recordId", request.RecordId);
                cmd.Parameters.AddWithValue("@isApproved", request.IsApproved);
                cmd.Parameters.AddWithValue("@isRejected", request.IsRejected);
                cmd.Parameters.AddWithValue("@rejectionRemark", string.IsNullOrWhiteSpace(request.RejectionRemark) ? DBNull.Value : request.RejectionRemark);
                cmd.Parameters.AddWithValue("@approvedBy", request.ApprovedBy);

                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0 ? Ok("Record updated successfully.") :
                                  StatusCode(500, "Failed to update record.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error updating approval/rejection", Error = ex.Message });
            }
        }


        [HttpGet("GetServiceDueSummary")]
        public async Task<IActionResult> GetServiceDueSummary(
    string filterType,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    int? month = null,
    int? year = null)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync();

                string dateCondition = filterType.ToLower() switch
                {
                    "date" or "week" when fromDate != null && toDate != null =>
                        $"next_service_date BETWEEN @fromDate AND @toDate",

                    "month" when month != null && year != null =>
                        $"DATE_PART('month', next_service_date) = @month AND DATE_PART('year', next_service_date) = @year",

                    "year" when year != null =>
                        $"DATE_PART('year', next_service_date) = @year",

                    _ => throw new ArgumentException("Invalid filter type or missing parameters")
                };

                string query = $@"
            SELECT am.asset_id, am.asset_name, am.last_service_date, am.next_service_date,
                   sr.is_approved, sr.is_rejected
            FROM asset.asset_master am
            LEFT JOIN asset.asset_service_records sr
              ON sr.assetid = am.asset_id
              AND sr.servicedate = am.last_service_date
            WHERE am.next_service_date IS NOT NULL
              AND (
                    am.next_service_date < CURRENT_DATE
                    OR ({dateCondition})
                  )
            ORDER BY am.next_service_date ASC;";

                using var cmd = new NpgsqlCommand(query, conn);

                if (fromDate != null) cmd.Parameters.AddWithValue("@fromDate", fromDate.Value.Date);
                if (toDate != null) cmd.Parameters.AddWithValue("@toDate", toDate.Value.Date);
                if (month != null) cmd.Parameters.AddWithValue("@month", month.Value);
                if (year != null) cmd.Parameters.AddWithValue("@year", year.Value);

                using var reader = await cmd.ExecuteReaderAsync();

                var results = new List<dynamic>();
                int overdueCount = 0;
                int upcomingCount = 0;

                while (await reader.ReadAsync())
                {
                    DateTime? nextServiceDate = reader.IsDBNull(reader.GetOrdinal("next_service_date"))
                        ? (DateTime?)null
                        : reader.GetDateTime(reader.GetOrdinal("next_service_date"));

                    string status = "Unknown";
                    if (nextServiceDate.HasValue)
                    {
                        if (nextServiceDate.Value < DateTime.Today)
                        {
                            status = "Overdue";
                            overdueCount++;
                        }
                        else
                        {
                            status = "Upcoming";
                            upcomingCount++;
                        }
                    }

                    // Get approval/rejection status
                    bool? isApproved = reader.IsDBNull(reader.GetOrdinal("is_approved")) ? (bool?)null :
                                       reader.GetBoolean(reader.GetOrdinal("is_approved"));
                    bool? isRejected = reader.IsDBNull(reader.GetOrdinal("is_rejected")) ? (bool?)null :
                                       reader.GetBoolean(reader.GetOrdinal("is_rejected"));

                    string approvalStatus = "Pending";
                    if (isApproved == true) approvalStatus = "Approved";
                    else if (isRejected == true) approvalStatus = "Rejected";

                    results.Add(new
                    {
                        AssetId = reader.GetInt32(reader.GetOrdinal("asset_id")),
                        AssetName = reader.IsDBNull(reader.GetOrdinal("asset_name")) ? null : reader.GetString(reader.GetOrdinal("asset_name")),
                        LastServiceDate = reader.IsDBNull(reader.GetOrdinal("last_service_date")) ? null :
                                          reader.GetDateTime(reader.GetOrdinal("last_service_date")).ToString("yyyy-MM-dd"),
                        NextServiceDate = nextServiceDate?.ToString("yyyy-MM-dd"),
                        Status = status,
                        ApprovalStatus = approvalStatus
                    });
                }

                var response = new
                {
                    Summary = new
                    {
                        Overdue = overdueCount,
                        Upcoming = upcomingCount,
                        Total = overdueCount + upcomingCount
                    },
                    Data = results
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving service summary", Error = ex.Message });
            }
        }
    }
}
