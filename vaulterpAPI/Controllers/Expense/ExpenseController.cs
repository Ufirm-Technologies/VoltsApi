using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Expense;

namespace vaulterpAPI.Controllers.expense
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpenseController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ExpenseController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection");
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ExpenseMaster expense)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var query = @"INSERT INTO attendance.expense_master 
                  (expense_type, expense_subtype, date_from, date_to, amount, description, bill_image, office_id, is_active, created_by, created_on) 
                  VALUES (@type, @subtype, @dateFrom, @dateTo, @amount, @desc, @bill, @office, @active, @createdBy, CURRENT_TIMESTAMP)
                  RETURNING id;";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@type", expense.ExpenseType);
            cmd.Parameters.AddWithValue("@subtype", (object?)expense.ExpenseSubtype ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dateFrom", expense.DateFrom);
            cmd.Parameters.AddWithValue("@dateTo", (object?)expense.DateTo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@amount", expense.Amount);
            cmd.Parameters.AddWithValue("@desc", (object?)expense.Description ?? DBNull.Value);
            if (!string.IsNullOrEmpty(expense.BillImage))
            {
                string base64 = expense.BillImage;

                // Remove possible data:image/...;base64, prefix
                if (base64.Contains(","))
                    base64 = base64.Substring(base64.IndexOf(",") + 1);

                cmd.Parameters.AddWithValue("@bill", Convert.FromBase64String(base64));
            }
            else
            {
                cmd.Parameters.AddWithValue("@bill", DBNull.Value);
            }


            cmd.Parameters.AddWithValue("@office", expense.OfficeId);
            cmd.Parameters.AddWithValue("@active", expense.IsActive);
            cmd.Parameters.AddWithValue("@createdBy", (object?)expense.CreatedBy ?? DBNull.Value);

            var id = await cmd.ExecuteScalarAsync();
            return Ok(new { id });
        }

        // READ BY OFFICE
        [HttpGet("by-office/{officeId}")]
        public async Task<IActionResult> GetByOfficeId(int officeId)
        {
            var expenses = new List<ExpenseMaster>();
            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var query = @"SELECT * FROM attendance.expense_master 
                          WHERE office_id = @officeId AND is_active = TRUE 
                          ORDER BY id DESC;";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@officeId", officeId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }

            return Ok(expenses);
        }

        // READ BY ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var query = @"SELECT * FROM attendance.expense_master WHERE id = @id;";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Ok(MapExpense(reader));
            }

            return NotFound(new { message = "Expense not found" });
        }

        // UPDATE
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ExpenseMaster expense)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var query = @"UPDATE attendance.expense_master 
                          SET expense_type=@type, expense_subtype=@subtype, date_from=@dateFrom, date_to=@dateTo, 
                              amount=@amount, description=@desc, bill_image=@bill, office_id=@office, 
                              is_active=@active, updated_by=@updatedBy, updated_on=CURRENT_TIMESTAMP
                          WHERE id=@id;";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@type", expense.ExpenseType);
            cmd.Parameters.AddWithValue("@subtype", (object?)expense.ExpenseSubtype ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dateFrom", expense.DateFrom);
            cmd.Parameters.AddWithValue("@dateTo", (object?)expense.DateTo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@amount", expense.Amount);
            cmd.Parameters.AddWithValue("@desc", (object?)expense.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@bill", !string.IsNullOrEmpty(expense.BillImage)
                                               ? Convert.FromBase64String(expense.BillImage)
                                               : DBNull.Value);
            cmd.Parameters.AddWithValue("@office", expense.OfficeId);
            cmd.Parameters.AddWithValue("@active", expense.IsActive);
            cmd.Parameters.AddWithValue("@updatedBy", (object?)expense.UpdatedBy ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0) return Ok(new { message = "Expense updated successfully" });

            return NotFound(new { message = "Expense not found" });
        }

        // SOFT DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var query = @"UPDATE attendance.expense_master 
                          SET is_active = FALSE, updated_on = CURRENT_TIMESTAMP
                          WHERE id = @id AND is_active = TRUE;";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0) return Ok(new { message = "Expense soft-deleted successfully" });

            return NotFound(new { message = "Expense not found or already inactive" });
        }

        // 🔹 Helper to map DB rows → ExpenseMaster
        private ExpenseMaster MapExpense(NpgsqlDataReader reader)
        {
            return new ExpenseMaster
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                ExpenseType = reader.GetString(reader.GetOrdinal("expense_type")),
                ExpenseSubtype = reader.IsDBNull(reader.GetOrdinal("expense_subtype")) ? null : reader.GetString(reader.GetOrdinal("expense_subtype")),
                DateFrom = reader.GetDateTime(reader.GetOrdinal("date_from")),
                DateTo = reader.IsDBNull(reader.GetOrdinal("date_to")) ? null : reader.GetDateTime(reader.GetOrdinal("date_to")),
                Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                BillImage = reader.IsDBNull(reader.GetOrdinal("bill_image")) ? null : Convert.ToBase64String((byte[])reader["bill_image"]),
                OfficeId = reader.GetInt32(reader.GetOrdinal("office_id")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? null : reader.GetInt32(reader.GetOrdinal("created_by")),
                CreatedOn = reader.IsDBNull(reader.GetOrdinal("created_on")) ? null : reader.GetDateTime(reader.GetOrdinal("created_on")),
                UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updated_by")) ? null : reader.GetInt32(reader.GetOrdinal("updated_by")),
                UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updated_on")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_on"))
            };
        }
    }
}
