using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using vaulterpAPI.Models.Employee;

namespace vaulterpAPI.Controllers.Employee
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmpOpsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public EmpOpsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ✅ Create new operation
        [HttpPost("create")]
        public IActionResult CreateOperation([FromBody] CreateOperationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OperationName) || request.OfficeId <= 0 || request.CreatedBy <= 0)
                return BadRequest(new { message = "Operation name, office ID, and created by are required." });

            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
            INSERT INTO master.operation_master 
            (operation_name, description, office_id, is_active, created_by, created_on, updated_on,operation_code)
            VALUES 
            (@operation_name, @description, @office_id, TRUE, @created_by, NOW(), NOW(),@operation_code)", conn);

                cmd.Parameters.AddWithValue("@operation_name", request.OperationName);
                cmd.Parameters.AddWithValue("@description", request.Description ?? "");
                cmd.Parameters.AddWithValue("@office_id", request.OfficeId);
                cmd.Parameters.AddWithValue("@created_by", request.CreatedBy);
                cmd.Parameters.AddWithValue("@operation_code", request.OperationCode ?? "");

                cmd.ExecuteNonQuery();

                return Ok(new { message = "Operation created successfully", status = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating operation", error = ex.Message });
            }
        }


        // ✅ Get all operations by officeId
        [HttpGet("operations-by-office")]
        public IActionResult GetAll([FromQuery] int officeId)
        {
            try
            {
                List<dynamic> operations = new();

                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    SELECT operation_id, operation_name, description, operation_code
                    FROM master.operation_master
                    WHERE is_active = TRUE AND office_id = @office_id", conn);

                cmd.Parameters.AddWithValue("@office_id", officeId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    operations.Add(new
                    {
                        OperationId = (int)reader["operation_id"],
                        OperationName = reader["operation_name"]?.ToString(),
                        Description = reader["description"]?.ToString(),
                        OperationCode = reader["operation_code"]?.ToString()
                    });
                }

                return Ok(operations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching operations", error = ex.Message });
            }
        }

        // ✅ Get assigned operations by employeeId
        [HttpGet("operations-by-employee")]
        public IActionResult GetOperationsByEmployee([FromQuery] int employeeId)
        {
            try
            {
                List<dynamic> ops = new();

                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    SELECT eo.id, eo.employee_id, eo.operation_id, o.operation_name, o.description
                    FROM master.employee_operation eo
                    JOIN master.operation_master o ON eo.operation_id = o.operation_id
                    WHERE eo.is_active = TRUE AND eo.employee_id = @employee_id", conn);

                cmd.Parameters.AddWithValue("@employee_id", employeeId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ops.Add(new
                    {
                        MappingId = (int)reader["id"],
                        EmployeeId = (int)reader["employee_id"],
                        OperationId = (int)reader["operation_id"],
                        OperationName = reader["operation_name"]?.ToString(),
                        Description = reader["description"]?.ToString()
                    });
                }

                return Ok(ops);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching employee operations", error = ex.Message });
            }
        }

        // ✅ Create/update mapping for multiple operations
        [HttpPost("map")]
        public IActionResult MapEmployeeToOperations([FromBody] EmployeeOperationMappingRequest request)
        {
            if (request.OperationIds == null || request.OperationIds.Count == 0)
                return BadRequest(new { message = "At least one operationId is required." });

            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                // Soft-delete old mappings
                var softDeleteCmd = new NpgsqlCommand(@"
                    UPDATE master.employee_operation 
                    SET is_active = FALSE, updated_by = @updated_by, updated_on = NOW()
                    WHERE employee_id = @employee_id AND is_active = TRUE", conn);

                softDeleteCmd.Parameters.AddWithValue("@employee_id", request.EmployeeId);
                softDeleteCmd.Parameters.AddWithValue("@updated_by", request.UpdatedBy);
                softDeleteCmd.ExecuteNonQuery();

                // Insert new mappings
                foreach (var operationId in request.OperationIds)
                {
                    var insertCmd = new NpgsqlCommand(@"
                        INSERT INTO master.employee_operation 
                        (employee_id, operation_id, is_active, created_by, created_on, updated_on)
                        VALUES 
                        (@employee_id, @operation_id, TRUE, @created_by, NOW(), NOW())", conn);

                    insertCmd.Parameters.AddWithValue("@employee_id", request.EmployeeId);
                    insertCmd.Parameters.AddWithValue("@operation_id", operationId);
                    insertCmd.Parameters.AddWithValue("@created_by", request.UpdatedBy);
                    insertCmd.ExecuteNonQuery();
                }

                return Ok(new { message = "Mappings updated successfully", status = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating mappings", error = ex.Message });
            }
        }

        // ❌ Soft-delete a single mapping
        [HttpDelete("{id}")]
        public IActionResult DeleteMapping(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    UPDATE master.employee_operation
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

        // ✅ Get all employees assigned to a specific operation
        [HttpGet("employees-by-operation")]
        public IActionResult GetEmployeesByOperation([FromQuery] int operationId)
        {
            try
            {
                List<dynamic> employees = new();

                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand(@"
            SELECT eo.id AS mapping_id, eo.employee_id, e.employee_name, e.email
            FROM master.employee_operation eo
            JOIN master.employee_master e ON eo.employee_id = e.employee_id
            WHERE eo.is_active = TRUE AND eo.operation_id = @operation_id", conn);

                cmd.Parameters.AddWithValue("@operation_id", operationId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    employees.Add(new
                    {
                        MappingId = (int)reader["mapping_id"],
                        EmployeeId = (int)reader["employee_id"],
                        EmployeeName = reader["employee_name"]?.ToString(),
                        Email = reader["email"]?.ToString()
                    });
                }

                return Ok(employees);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching employees for operation", error = ex.Message });
            }
        }

    }
}
