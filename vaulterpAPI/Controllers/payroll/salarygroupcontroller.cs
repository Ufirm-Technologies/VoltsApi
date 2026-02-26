using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using vaulterpAPI.Models.Payroll;

namespace vaulterpAPI.Controllers.payroll
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalaryAllowanceController : ControllerBase
    {
        private readonly IConfiguration _config;
        public SalaryAllowanceController(IConfiguration config) => _config = config;
        private string ConnStr() => _config.GetConnectionString("DefaultConnection");

        // ---------------------------
        // GET Salary Group (by ID) with linked AD + Formula
        // ---------------------------
        [HttpGet("{salaryGroupId}")]
        public async Task<IActionResult> GetById(int salaryGroupId)
        {
            var model = new SalaryAllowanceDto { AllowancesDeductions = new List<AllowanceDeductionDto>() };

            using var conn = new NpgsqlConnection(ConnStr());
            await conn.OpenAsync();

            var query = @"
SELECT
  sgm.salarygroup_id,
  sgm.salary_group,
  sgm.basic_salary,
  sgm.office_id,
  sgm.created_on,
  sgm.created_by,
  sgm.updated_on,
  sgm.updated_by,
  sgm.is_active,
  sgm.totalworkingdays,
  sgm.shifthours,

  link.id AS link_id,
  link.ad_id,
  link.fixed_amount,
  link.formula_id,
  link.calculatedamount,
  link.isactive AS link_isactive,

  fm.id AS formula_id,
  fm.name AS formula_name,
  fm.formula AS formula_text,
  fm.fixedvalue AS formula_fixedvalue,
  fm.isactive AS formula_isactive

FROM payroll.salary_group_master sgm
LEFT JOIN payroll.salarygroup_allowancedeductionlink link
  ON link.salarygroup_id = sgm.salarygroup_id AND link.isactive = TRUE
LEFT JOIN payroll.formula_master fm
  ON fm.id = link.formula_id
WHERE sgm.salarygroup_id = @salaryGroupId;
";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@salaryGroupId", salaryGroupId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (model.SalaryGroup_ID == 0)
                {
                    model.SalaryGroup_ID = reader.GetInt32(reader.GetOrdinal("salarygroup_id"));
                    model.SalaryGroup = reader.IsDBNull(reader.GetOrdinal("salary_group")) ? null : reader.GetString(reader.GetOrdinal("salary_group"));
                    model.BaseSalary = reader.IsDBNull(reader.GetOrdinal("basic_salary")) ? 0 : reader.GetDecimal(reader.GetOrdinal("basic_salary"));
                    model.OfficeId = reader.GetInt32(reader.GetOrdinal("office_id"));
                    model.CreatedOn = reader.IsDBNull(reader.GetOrdinal("created_on")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("created_on"));
                    model.CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("created_by"));
                    model.UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updated_on")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updated_on"));
                    model.UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updated_by")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updated_by"));
                    model.IsActive = reader.IsDBNull(reader.GetOrdinal("is_active")) ? true : reader.GetBoolean(reader.GetOrdinal("is_active"));
                    model.TotalWorkingDays = reader.IsDBNull(reader.GetOrdinal("totalworkingdays")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("totalworkingdays"));
                    model.ShiftHours = reader.IsDBNull(reader.GetOrdinal("shifthours")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("shifthours"));
                }

                if (!reader.IsDBNull(reader.GetOrdinal("link_id")))
                {
                    var ad = new AllowanceDeductionDto
                    {
                        LinkId = reader.GetInt32(reader.GetOrdinal("link_id")),
                        AD_Id = reader.IsDBNull(reader.GetOrdinal("ad_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("ad_id")),
                        FixedAmount = reader.IsDBNull(reader.GetOrdinal("fixed_amount")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("fixed_amount")),
                        CalculatedAmount = reader.IsDBNull(reader.GetOrdinal("calculatedamount")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("calculatedamount")),
                        FormulaId = reader.IsDBNull(reader.GetOrdinal("formula_id")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("formula_id")),
                        Formula = reader.IsDBNull(reader.GetOrdinal("formula_text")) ? null : reader.GetString(reader.GetOrdinal("formula_text")),
                        IsActive = reader.IsDBNull(reader.GetOrdinal("link_isactive")) ? true : reader.GetBoolean(reader.GetOrdinal("link_isactive")),
                    };

                    model.AllowancesDeductions.Add(ad);
                }
            }

            if (model.SalaryGroup_ID == 0) return NotFound(new { message = "Salary group not found" });
            return Ok(model);
        }

        // ---------------------------
        // GET BY OFFICE (all Salary Groups for an office)
        // ---------------------------
        [HttpGet("by-office/{officeId}")]
        public async Task<IActionResult> GetByOffice(int officeId)
        {
            var result = new List<SalaryAllowanceDto>();

            using var conn = new NpgsqlConnection(ConnStr());
            await conn.OpenAsync();

            var query = @"
SELECT
  sgm.salarygroup_id,
  sgm.salary_group,
  sgm.basic_salary,
  sgm.office_id,
  sgm.created_on,
  sgm.created_by,
  sgm.updated_on,
  sgm.updated_by,
  sgm.is_active,
  sgm.totalworkingdays,
  sgm.shifthours,

  link.id AS link_id,
  link.ad_id,
  link.fixed_amount,
  link.formula_id,
  link.calculatedamount,
  link.isactive AS link_isactive,

  fm.id AS formula_id,
  fm.name AS formula_name,
  fm.formula AS formula_text,
  fm.fixedvalue AS formula_fixedvalue,
  fm.isactive AS formula_isactive

FROM payroll.salary_group_master sgm
LEFT JOIN payroll.salarygroup_allowancedeductionlink link
  ON link.salarygroup_id = sgm.salarygroup_id AND link.isactive = TRUE
LEFT JOIN payroll.formula_master fm
  ON fm.id = link.formula_id
WHERE sgm.office_id = @officeId
  AND sgm.is_active = TRUE
ORDER BY sgm.salarygroup_id ASC;
";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@officeId", officeId);

            using var reader = await cmd.ExecuteReaderAsync();
            var map = new Dictionary<int, SalaryAllowanceDto>();

            while (await reader.ReadAsync())
            {
                int sgId = reader.GetInt32(reader.GetOrdinal("salarygroup_id"));
                if (!map.ContainsKey(sgId))
                {
                    map[sgId] = new SalaryAllowanceDto
                    {
                        SalaryGroup_ID = sgId,
                        SalaryGroup = reader.IsDBNull(reader.GetOrdinal("salary_group")) ? null : reader.GetString(reader.GetOrdinal("salary_group")),
                        BaseSalary = reader.IsDBNull(reader.GetOrdinal("basic_salary")) ? 0 : reader.GetDecimal(reader.GetOrdinal("basic_salary")),
                        OfficeId = reader.GetInt32(reader.GetOrdinal("office_id")),
                        CreatedOn = reader.IsDBNull(reader.GetOrdinal("created_on")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("created_on")),
                        CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("created_by")),
                        UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updated_on")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updated_on")),
                        UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updated_by")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updated_by")),
                        IsActive = reader.IsDBNull(reader.GetOrdinal("is_active")) ? true : reader.GetBoolean(reader.GetOrdinal("is_active")),
                        TotalWorkingDays = reader.IsDBNull(reader.GetOrdinal("totalworkingdays")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("totalworkingdays")),
                        ShiftHours = reader.IsDBNull(reader.GetOrdinal("shifthours")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("shifthours")),
                        AllowancesDeductions = new List<AllowanceDeductionDto>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("link_id")))
                {
                    map[sgId].AllowancesDeductions.Add(new AllowanceDeductionDto
                    {
                        LinkId = reader.GetInt32(reader.GetOrdinal("link_id")),
                        AD_Id = reader.IsDBNull(reader.GetOrdinal("ad_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("ad_id")),
                        FixedAmount = reader.IsDBNull(reader.GetOrdinal("fixed_amount")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("fixed_amount")),
                        CalculatedAmount = reader.IsDBNull(reader.GetOrdinal("calculatedamount")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("calculatedamount")),
                        FormulaId = reader.IsDBNull(reader.GetOrdinal("formula_id")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("formula_id")),
                        Formula = reader.IsDBNull(reader.GetOrdinal("formula_text")) ? null : reader.GetString(reader.GetOrdinal("formula_text")),
                        IsActive = reader.IsDBNull(reader.GetOrdinal("link_isactive")) ? true : reader.GetBoolean(reader.GetOrdinal("link_isactive")),
                    });
                }
            }

            result.AddRange(map.Values);
            return Ok(result);
        }

        // ---------------------------
        // CREATE salary group + links (with formula creation when provided)
        // ---------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SalaryAllowanceDto model)
        {
            if (model == null) return BadRequest("Invalid body");

            using var conn = new NpgsqlConnection(ConnStr());
            await conn.OpenAsync();
            using var tran = await conn.BeginTransactionAsync();

            try
            {
                // 1) insert salary group
                var insertSG = @"
INSERT INTO payroll.salary_group_master
  (salary_group, basic_salary, office_id, created_on, created_by, is_active, totalworkingdays, shifthours)
VALUES
  (@salary_group, @basic_salary, @office_id, CURRENT_DATE, @created_by, TRUE, @totalworkingdays, @shifthours)
RETURNING salarygroup_id;
";
                using var cmdSG = new NpgsqlCommand(insertSG, conn, tran);
                cmdSG.Parameters.AddWithValue("@salary_group", (object)model.SalaryGroup ?? DBNull.Value);
                cmdSG.Parameters.AddWithValue("@basic_salary", (object)model.BaseSalary ?? DBNull.Value);
                cmdSG.Parameters.AddWithValue("@office_id", model.OfficeId);
                cmdSG.Parameters.AddWithValue("@created_by", (object)model.CreatedBy ?? DBNull.Value);
                cmdSG.Parameters.AddWithValue("@totalworkingdays", (object)model.TotalWorkingDays ?? DBNull.Value);
                cmdSG.Parameters.AddWithValue("@shifthours", (object)model.ShiftHours ?? DBNull.Value);

                var newIdObj = await cmdSG.ExecuteScalarAsync();
                var newSalaryGroupId = Convert.ToInt32(newIdObj);
                model.SalaryGroup_ID = newSalaryGroupId;

                // 2) insert links & formulas
                if (model.AllowancesDeductions != null)
                {
                    foreach (var item in model.AllowancesDeductions)
                    {
                        int? formulaId = item.FormulaId;

                        // if formula text is supplied but no formulaId -> insert formula
                        if (!string.IsNullOrWhiteSpace(item.Formula) && !formulaId.HasValue)
                        {
                            var insFormula = @"
INSERT INTO payroll.formula_master (name, formula, isactive, createdon, officeid, fixedvalue)
VALUES (@name, @formula, 1, CURRENT_TIMESTAMP, @officeid, NULL)
RETURNING id;
";
                            using var cmdF = new NpgsqlCommand(insFormula, conn, tran);
                            cmdF.Parameters.AddWithValue("@name", $"{model.SalaryGroup}-{item.ADDisplayNameOrId()}");
                            cmdF.Parameters.AddWithValue("@formula", item.Formula);
                            cmdF.Parameters.AddWithValue("@officeid", model.OfficeId);
                            var fid = await cmdF.ExecuteScalarAsync();
                            formulaId = Convert.ToInt32(fid);
                        }

                        // insert link
                        var insLink = @"
INSERT INTO payroll.salarygroup_allowancedeductionlink
  (salarygroup_id, ad_id, office_id, calculatedamount, formula_id, fixed_amount, created_on, created_by, isactive)
VALUES
  (@salarygroup_id, @ad_id, @office_id, @calculatedamount, @formula_id, @fixed_amount, CURRENT_DATE, @created_by, TRUE)
RETURNING id;
";
                        using var cmdL = new NpgsqlCommand(insLink, conn, tran);
                        cmdL.Parameters.AddWithValue("@salarygroup_id", newSalaryGroupId);
                        cmdL.Parameters.AddWithValue("@ad_id", item.AD_Id);
                        cmdL.Parameters.AddWithValue("@office_id", model.OfficeId);
                        cmdL.Parameters.AddWithValue("@calculatedamount", (object)item.CalculatedAmount ?? DBNull.Value);
                        cmdL.Parameters.AddWithValue("@formula_id", (object)formulaId ?? DBNull.Value);
                        cmdL.Parameters.AddWithValue("@fixed_amount", (object)item.FixedAmount ?? DBNull.Value);
                        cmdL.Parameters.AddWithValue("@created_by", (object)model.CreatedBy ?? DBNull.Value);

                        var linkIdObj = await cmdL.ExecuteScalarAsync();
                        item.LinkId = Convert.ToInt32(linkIdObj);
                    }
                }

                await tran.CommitAsync();

                return Ok(new { message = "Salary group created successfully", data = model });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return BadRequest(new { message = "Create failed", error = ex.Message });
            }
        }

        // ---------------------------
        // UPDATE salary group + re-insert links + formula handling
        // ---------------------------
        [HttpPut("{salaryGroupId}")]
        public async Task<IActionResult> Update(int salaryGroupId, [FromBody] SalaryAllowanceDto model)
        {
            if (model == null) return BadRequest("Invalid body");

            using var conn = new NpgsqlConnection(ConnStr());
            await conn.OpenAsync();
            using var tran = await conn.BeginTransactionAsync();

            try
            {
                // 1) Update salary group header
                var updateSG = @"
UPDATE payroll.salary_group_master
SET salary_group = @salary_group,
    basic_salary = @basic_salary,
    office_id = @office_id,
    updated_on = CURRENT_DATE,
    updated_by = @updated_by,
    totalworkingdays = @totalworkingdays,
    shifthours = @shifthours
WHERE salarygroup_id = @salarygroup_id;
";
                using var cmdUpd = new NpgsqlCommand(updateSG, conn, tran);
                cmdUpd.Parameters.AddWithValue("@salarygroup_id", salaryGroupId);
                cmdUpd.Parameters.AddWithValue("@salary_group", (object)model.SalaryGroup ?? DBNull.Value);
                cmdUpd.Parameters.AddWithValue("@basic_salary", (object)model.BaseSalary ?? DBNull.Value);
                cmdUpd.Parameters.AddWithValue("@office_id", model.OfficeId);
                cmdUpd.Parameters.AddWithValue("@updated_by", (object)model.UpdatedBy ?? (object)model.CreatedBy ?? DBNull.Value);
                cmdUpd.Parameters.AddWithValue("@totalworkingdays", (object)model.TotalWorkingDays ?? DBNull.Value);
                cmdUpd.Parameters.AddWithValue("@shifthours", (object)model.ShiftHours ?? DBNull.Value);

                await cmdUpd.ExecuteNonQueryAsync();

                // 2) Delete existing links (soft delete or hard delete - implementing hard delete to match original re-insert behavior)
                var delLinks = @"
DELETE FROM payroll.salarygroup_allowancedeductionlink
WHERE salarygroup_id = @salarygroup_id;
";
                using var cmdDel = new NpgsqlCommand(delLinks, conn, tran);
                cmdDel.Parameters.AddWithValue("@salarygroup_id", salaryGroupId);
                await cmdDel.ExecuteNonQueryAsync();

                // 3) Re-insert links and handle formulas
                if (model.AllowancesDeductions != null)
                {
                    foreach (var item in model.AllowancesDeductions)
                    {
                        int? formulaId = item.FormulaId;

                        // CASE A: Update existing formula if formula text provided and formulaId present
                        if (!string.IsNullOrWhiteSpace(item.Formula) && formulaId.HasValue)
                        {
                            var updFormula = @"
UPDATE payroll.formula_master
SET name = @name,
    formula = @formula,
    fixedvalue = NULL,
    updatedon = CURRENT_TIMESTAMP
WHERE id = @formula_id;
";
                            using var cmdUpdF = new NpgsqlCommand(updFormula, conn, tran);
                            cmdUpdF.Parameters.AddWithValue("@name", $"{model.SalaryGroup}-{item.ADDisplayNameOrId()}");
                            cmdUpdF.Parameters.AddWithValue("@formula", item.Formula);
                            cmdUpdF.Parameters.AddWithValue("@formula_id", formulaId.Value);
                            await cmdUpdF.ExecuteNonQueryAsync();
                        }
                        // CASE B: Insert new formula if text exists and formulaId not present
                        else if (!string.IsNullOrWhiteSpace(item.Formula) && !formulaId.HasValue)
                        {
                            var insFormula = @"
INSERT INTO payroll.formula_master (name, formula, isactive, createdon, officeid, fixedvalue)
VALUES (@name, @formula, 1, CURRENT_TIMESTAMP, @officeid, NULL)
RETURNING id;
";
                            using var cmdInsF = new NpgsqlCommand(insFormula, conn, tran);
                            cmdInsF.Parameters.AddWithValue("@name", $"{model.SalaryGroup}-{item.ADDisplayNameOrId()}");
                            cmdInsF.Parameters.AddWithValue("@formula", item.Formula);
                            cmdInsF.Parameters.AddWithValue("@officeid", model.OfficeId);
                            var fid = await cmdInsF.ExecuteScalarAsync();
                            formulaId = Convert.ToInt32(fid);
                        }
                        // CASE C: Formula removed but formulaId present -> convert to fixed value
                        else if (string.IsNullOrWhiteSpace(item.Formula) && formulaId.HasValue)
                        {
                            var clearFormula = @"
UPDATE payroll.formula_master
SET formula = NULL,
    fixedvalue = @fixedvalue,
    updatedon = CURRENT_TIMESTAMP
WHERE id = @formula_id;
";
                            using var cmdClearF = new NpgsqlCommand(clearFormula, conn, tran);
                            cmdClearF.Parameters.AddWithValue("@fixedvalue", (object)item.FixedAmount ?? DBNull.Value);
                            cmdClearF.Parameters.AddWithValue("@formula_id", formulaId.Value);
                            await cmdClearF.ExecuteNonQueryAsync();

                            formulaId = null; // unlink formula from new link
                        }

                        // Insert link
                        var insLink = @"
INSERT INTO payroll.salarygroup_allowancedeductionlink
  (salarygroup_id, ad_id, office_id, calculatedamount, formula_id, fixed_amount, created_on, created_by, isactive)
VALUES
  (@salarygroup_id, @ad_id, @office_id, @calculatedamount, @formula_id, @fixed_amount, CURRENT_DATE, @created_by, TRUE)
RETURNING id;
";
                        using var cmdL = new NpgsqlCommand(insLink, conn, tran);
                        cmdL.Parameters.AddWithValue("@salarygroup_id", salaryGroupId);
                        cmdL.Parameters.AddWithValue("@ad_id", item.AD_Id);
                        cmdL.Parameters.AddWithValue("@office_id", model.OfficeId);
                        cmdL.Parameters.AddWithValue("@calculatedamount", (object)item.CalculatedAmount ?? DBNull.Value);
                        cmdL.Parameters.AddWithValue("@formula_id", (object)formulaId ?? DBNull.Value);
                        cmdL.Parameters.AddWithValue("@fixed_amount", (object)item.FixedAmount ?? DBNull.Value);
                        cmdL.Parameters.AddWithValue("@created_by", (object)model.UpdatedBy ?? (object)model.CreatedBy ?? DBNull.Value);

                        var linkIdObj = await cmdL.ExecuteScalarAsync();
                        item.LinkId = Convert.ToInt32(linkIdObj);
                    }
                }

                await tran.CommitAsync();
                return Ok(new { message = "Salary group updated successfully", data = model });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return BadRequest(new { message = "Update failed", error = ex.Message });
            }
        }

        // ---------------------------
        // DELETE Salary Group (soft delete + deactivate linked formulas by name prefix)
        // ---------------------------
        [HttpDelete("{salaryGroupId}")]
        public async Task<IActionResult> Delete(int salaryGroupId)
        {
            using var conn = new NpgsqlConnection(ConnStr());
            await conn.OpenAsync();
            using var tran = await conn.BeginTransactionAsync();

            try
            {
                // fetch salary group name
                string sgName = null;
                var getNameQ = "SELECT salary_group FROM payroll.salary_group_master WHERE salarygroup_id = @id;";
                using (var cmdG = new NpgsqlCommand(getNameQ, conn, tran))
                {
                    cmdG.Parameters.AddWithValue("@id", salaryGroupId);
                    var obj = await cmdG.ExecuteScalarAsync();
                    sgName = obj?.ToString();
                }

                // soft delete salary group
                var q1 = "UPDATE payroll.salary_group_master SET is_active = FALSE, updated_on = CURRENT_DATE WHERE salarygroup_id = @id;";
                using (var cmd1 = new NpgsqlCommand(q1, conn, tran))
                {
                    cmd1.Parameters.AddWithValue("@id", salaryGroupId);
                    await cmd1.ExecuteNonQueryAsync();
                }

                // soft delete links
                var q2 = "UPDATE payroll.salarygroup_allowancedeductionlink SET isactive = FALSE, updated_on = CURRENT_DATE WHERE salarygroup_id = @id;";
                using (var cmd2 = new NpgsqlCommand(q2, conn, tran))
                {
                    cmd2.Parameters.AddWithValue("@id", salaryGroupId);
                    await cmd2.ExecuteNonQueryAsync();
                }

                // deactivate formulas that start with 'SGName-' if SGName known
                if (!string.IsNullOrWhiteSpace(sgName))
                {
                    var q3 = "UPDATE payroll.formula_master SET isactive = 0, updatedon = CURRENT_TIMESTAMP WHERE name LIKE @pattern;";
                    using (var cmd3 = new NpgsqlCommand(q3, conn, tran))
                    {
                        cmd3.Parameters.AddWithValue("@pattern", sgName + "-%");
                        await cmd3.ExecuteNonQueryAsync();
                    }
                }

                await tran.CommitAsync();
                return Ok(new { message = "Deleted (soft) successfully" });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return BadRequest(new { message = "Delete failed", error = ex.Message });
            }
        }
    }
}
