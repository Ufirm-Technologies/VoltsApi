using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace vaulterpAPI.Controllers.Planning
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkInProcessController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public WorkInProcessController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        [HttpGet("aggregate/{officeId}")]
        public async Task<ActionResult<IEnumerable<WorkInProcessDto>>> GetAggregatedData(int officeId)
        {
            var list = new List<WorkInProcessDto>();
            using var connection = new NpgsqlConnection(GetConnectionString());
            await connection.OpenAsync();

            string sql = @"
                SELECT DISTINCT
                    ps.internal_work_order_id AS internalWorkOrder,
                    o.operation_name AS operationName,
                    ps.plan_date AS entryDate,
                    ps.achieved AS targetAchievedTillDate,    
                    p.product_name AS productName,
                    jc.take_up_drum_size AS takeUpDrumSize,
                    wo.board_name AS boardName,
                    pm.name AS partyName,
                    iwo.quantity AS qty
                FROM planning.daily_planning_sheet ps
                INNER JOIN work_order.internal_work_order iwo 
                    ON ps.internal_work_order_id = iwo.id
                INNER JOIN planning.job_card jc
                    ON iwo.id = jc.internal_wo
                INNER JOIN work_order.work_order_master wo
                    ON iwo.woid = wo.id
                INNER JOIN work_order.work_order_product wp
                    ON wo.id = wp.wo_id
                INNER JOIN planning.contruction c
                    ON c.internal_woid = jc.internal_wo 
                    AND c.operation_id = jc.operation_id
                INNER JOIN master.operation_master o
                    ON o.operation_id = ps.operation_id
                INNER JOIN work_order.product_master p
                    ON p.id = c.product_id
                INNER JOIN work_order.partymaster pm
                    ON pm.id = wo.party_id
                WHERE ps.office_id = @OfficeId
                  AND ps.is_active = true 
                  AND c.is_active = true 
                  AND jc.is_active = true
                GROUP BY ps.achieved,
                         ps.internal_work_order_id,
                         o.operation_name,
                         ps.plan_date,
                         p.product_name,
                         jc.take_up_drum_size,
                         wo.board_name,
                         pm.name,
                         iwo.quantity
                ORDER BY ps.internal_work_order_id, ps.plan_date ASC;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@OfficeId", officeId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new WorkInProcessDto
                {
                    InternalWorkOrder = reader.GetInt32(reader.GetOrdinal("internalWorkOrder")),
                    OperationName = reader["operationName"] as string,
                    EntryDate = reader.GetDateTime(reader.GetOrdinal("entryDate")),
                    TargetAchievedTillDate = reader.GetInt32(reader.GetOrdinal("targetAchievedTillDate")),
                    ProductName = reader["productName"] as string,
                    TakeUpDrumSize = reader["takeUpDrumSize"] as string,
                    BoardName = reader["boardName"] as string,
                    PartyID = reader["partyName"] as string,
                    Qty = reader.GetInt32(reader.GetOrdinal("qty"))
                });
            }
            return Ok(list);
        }


        [HttpGet("aggregate-summary/{officeId}")]
        public async Task<ActionResult<IEnumerable<WorkInProcessAggregateDto>>> GetAggregatedSummary(int officeId)
        {
            var list = new List<WorkInProcessAggregateDto>();
            using var connection = new NpgsqlConnection(GetConnectionString());
            await connection.OpenAsync();

            string sql = @"
        SELECT 
            ps.internal_work_order_id AS internalWorkOrder,
            p.product_name AS productName,
            SUM(ps.achieved) AS totalTargetAchievedTillDate,
iwo.quantity AS totalDeliverable
        FROM planning.daily_planning_sheet ps
        INNER JOIN work_order.internal_work_order iwo 
            ON ps.internal_work_order_id = iwo.id
        INNER JOIN planning.job_card jc
            ON iwo.id = jc.internal_wo
        INNER JOIN planning.contruction c
            ON c.internal_woid = jc.internal_wo 
            AND c.operation_id = jc.operation_id
        INNER JOIN work_order.product_master p
            ON p.id = c.product_id
        WHERE ps.office_id = @OfficeId
          AND ps.is_active = true 
          AND c.is_active = true 
          AND jc.is_active = true
          AND ps.plan_date <= CURRENT_DATE
        GROUP BY ps.internal_work_order_id, p.product_name,iwo.quantity
        ORDER BY ps.internal_work_order_id;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@OfficeId", officeId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new WorkInProcessAggregateDto
                {
                    InternalWorkOrder = reader.GetInt32(reader.GetOrdinal("internalWorkOrder")),
                    ProductName = reader["productName"] as string,
                    TotalTargetAchievedTillDate = reader.GetInt32(reader.GetOrdinal("totalTargetAchievedTillDate")),
                    TotalDeliverable = reader.GetInt32(reader.GetOrdinal("totalDeliverable"))
                });
            }

            return Ok(list);
        }
    }
public class WorkInProcessAggregateDto
    {
        public int InternalWorkOrder { get; set; }
        public string ProductName { get; set; }
        public int TotalTargetAchievedTillDate { get; set; }
        public int TotalDeliverable { get; set; }
    }


    // DTO class
    public class WorkInProcessDto
    {
        public int InternalWorkOrder { get; set; }
        public string OperationName { get; set; }
        public System.DateTime EntryDate { get; set; }
        public int TargetAchievedTillDate { get; set; }
        public string ProductName { get; set; }
        public string TakeUpDrumSize { get; set; }
        public string BoardName { get; set; }
        public string PartyID { get; set; }
        public int Qty { get; set; }
    }
}
