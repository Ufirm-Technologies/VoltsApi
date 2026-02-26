using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vaulterpAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssetDowntimeController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AssetDowntimeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        [HttpGet("finalreport")]
        public async Task<IActionResult> GetFinalAssetDowntimeReport(int officeId)
        {
            // store asset name + downtime together
            var assetTotals = new Dictionary<int, (string assetName, TimeSpan totalSpare, TimeSpan totalService)>();

            var query = @"
                SELECT 
                    COALESCE(a.asset_id, asr.assetid, m.asset_id) AS asset_id,
                    COALESCE(a.asset_name, '') AS assetname,
                    m.issue_date, 
                    m.actual_return_date, 
                    asr.days, 
                    asr.duration
                FROM asset.asset_master a
                FULL OUTER JOIN asset.asset_service_records as asr 
                    ON a.asset_id = asr.assetid
                FULL OUTER JOIN asset.asset_spare_maintenance m 
                    ON m.asset_id = COALESCE(a.asset_id, asr.assetid)
                WHERE a.office_id = @officeId";

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@officeId", officeId);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int assetId = reader.GetInt32(0);
                string assetName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                DateTime? checkIn = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2);
                DateTime? checkOut = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
                int serviceDays = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                TimeSpan serviceDuration = reader.IsDBNull(5) ? TimeSpan.Zero : reader.GetTimeSpan(5);

                if (!assetTotals.ContainsKey(assetId))
                    assetTotals[assetId] = (assetName, TimeSpan.Zero, TimeSpan.Zero);

                var (storedName, totalSpare, totalService) = assetTotals[assetId];

                // Spare downtime
                if (checkIn.HasValue && checkOut.HasValue)
                    totalSpare += checkOut.Value - checkIn.Value;

                // Service duration
                totalService += TimeSpan.FromDays(serviceDays) + serviceDuration;

                assetTotals[assetId] = (storedName, totalSpare, totalService);
            }

            // Prepare final report
            var report = assetTotals.Select(kvp =>
            {
                var (assetName, totalSpare, totalService) = kvp.Value;
                var totalDowntime = totalSpare + totalService;

                return new
                {
                    AssetId = kvp.Key,
                    AssetName = assetName,
                    TotalSpareDowntime = $"{(int)totalSpare.TotalHours}h {totalSpare.Minutes}m",
                    TotalServiceDuration = $"{totalService.Days}d {totalService.Hours}h {totalService.Minutes}m",
                    TotalAssetDowntime = $"{totalDowntime.Days}d {totalDowntime.Hours}h {totalDowntime.Minutes}m",
                    TotalAssetDowntimeMinutes = (int)totalDowntime.TotalMinutes
                };
            })
            .OrderByDescending(r => r.TotalAssetDowntimeMinutes)
            .Select(r => new
            {
                r.AssetId,
                r.AssetName,
                r.TotalSpareDowntime,
                r.TotalServiceDuration,
                r.TotalAssetDowntime
            })
            .ToList();

            return Ok(report);
        }
    }
}
