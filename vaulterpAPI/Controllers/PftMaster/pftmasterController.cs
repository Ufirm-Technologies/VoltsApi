using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.PFTMaster;

namespace vaulterpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PftMasterController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PftMasterController(IConfiguration configuration)
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
            List<PftMaster> list = new();

            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();

                string query = @"SELECT pftid, stateid, amountfrom, amountto, pftamount, createdon, updatedon 
                                 FROM payroll.pftmaster ORDER BY pftid DESC";

                using (var cmd = new NpgsqlCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PftMaster
                        {
                            PftId = reader.GetInt32(0),
                            StateId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                            AmountFrom = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                            AmountTo = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                            PftAmount = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
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
            PftMaster? obj = null;

            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();

                string query = @"SELECT pftid, stateid, amountfrom, amountto, pftamount, createdon, updatedon
                                 FROM payroll.pftmaster WHERE pftid = @id";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            obj = new PftMaster
                            {
                                PftId = reader.GetInt32(0),
                                StateId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                                AmountFrom = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                                AmountTo = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                                PftAmount = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
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
        public IActionResult Create(PftMaster model)
        {
            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();

                string query = @"INSERT INTO payroll.pftmaster
                                (stateid, amountfrom, amountto, pftamount, createdon, updatedon)
                                VALUES (@stateid, @amountfrom, @amountto, @pftamount, NOW(), NOW())
                                RETURNING pftid;";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@stateid", model.StateId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@amountfrom", model.AmountFrom ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@amountto", model.AmountTo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@pftamount", model.PftAmount ?? (object)DBNull.Value);

                    int newId = (int)cmd.ExecuteScalar();
                    return Ok(new { message = "Inserted Successfully", pftid = newId });
                }
            }
        }

        // ============================
        //          UPDATE
        // ============================
        [HttpPut("{id}")]
        public IActionResult Update(int id, PftMaster model)
        {
            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();

                string query = @"UPDATE payroll.pftmaster SET
                                 stateid = @stateid,
                                 amountfrom = @amountfrom,
                                 amountto = @amountto,
                                 pftamount = @pftamount,
                                 updatedon = NOW()
                                 WHERE pftid = @id";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@stateid", model.StateId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@amountfrom", model.AmountFrom ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@amountto", model.AmountTo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@pftamount", model.PftAmount ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Updated Successfully" });
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

                string query = @"DELETE FROM payroll.pftmaster WHERE pftid = @id";

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