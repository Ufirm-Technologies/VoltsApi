using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.LWFMaster;

namespace vaulterpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LwfMasterController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LwfMasterController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection");
        }

        // ============================
        //        GET ALL
        // ============================
        [HttpGet]
        public IActionResult GetAll()
        {
            List<LwfMaster> list = new List<LwfMaster>();

            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();
                string query = @"SELECT lwfid, stateid, lwfamount, employeeamount, employeramount, createdon, updatedon 
                                 FROM payroll.lwfmaster ORDER BY lwfid DESC";

                using (var cmd = new NpgsqlCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new LwfMaster
                        {
                            LwfId = reader.GetInt32(0),
                            StateId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                            LwfAmount = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                            EmployeeAmount = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                            EmployerAmount = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                            CreatedOn = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                            UpdatedOn = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
                        });
                    }
                }
            }

            return Ok(list);
        }

        // ============================
        //        GET BY ID
        // ============================
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            LwfMaster obj = null;

            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();
                string query = @"SELECT lwfid, stateid, lwfamount, employeeamount, employeramount, createdon, updatedon
                                 FROM payroll.lwfmaster WHERE lwfid = @id";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            obj = new LwfMaster
                            {
                                LwfId = reader.GetInt32(0),
                                StateId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                                LwfAmount = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                                EmployeeAmount = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                                EmployerAmount = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                                CreatedOn = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                                UpdatedOn = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
                            };
                        }
                    }
                }
            }

            if (obj == null)
                return NotFound();

            return Ok(obj);
        }

        // ============================
        //          CREATE
        // ============================
        [HttpPost]
        public IActionResult Create(LwfMaster model)
        {
            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();

                string query = @"INSERT INTO payroll.lwfmaster 
                                (stateid, lwfamount, employeeamount, employeramount, createdon, updatedon)
                                VALUES (@stateid, @lwfamount, @employeeamount, @employeramount, NOW(), NOW())
                                RETURNING lwfid;";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@stateid", model.StateId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@lwfamount", model.LwfAmount ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@employeeamount", model.EmployeeAmount ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@employeramount", model.EmployerAmount ?? (object)DBNull.Value);

                    int newId = (int)cmd.ExecuteScalar();
                    return Ok(new { message = "Inserted Successfully", lwfid = newId });
                }
            }
        }

        // ============================
        //          UPDATE
        // ============================
        [HttpPut("{id}")]
        public IActionResult Update(int id, LwfMaster model)
        {
            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();

                string query = @"UPDATE payroll.lwfmaster SET 
                                 stateid = @stateid,
                                 lwfamount = @lwfamount,
                                 employeeamount = @employeeamount,
                                 employeramount = @employeramount,
                                 updatedon = NOW()
                                 WHERE lwfid = @id";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@stateid", model.StateId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@lwfamount", model.LwfAmount ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@employeeamount", model.EmployeeAmount ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@employeramount", model.EmployerAmount ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "Updated Successfully" });
                }
            }
        }

        // ============================
        //          DELETE
        // ============================
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();

                string query = @"DELETE FROM payroll.lwfmaster WHERE lwfid = @id";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Deleted Successfully" });
        }
    }
}
