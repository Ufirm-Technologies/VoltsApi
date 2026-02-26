using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.CountryMaster;

namespace vaulterpAPI.Controllers.CountryMasters
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountryController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CountryController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // -------------------------------------------------------------
        // 1. GET ALL COUNTRIES
        // -------------------------------------------------------------
        [HttpGet]
        public IActionResult GetCountries()
        {
            List<CountryMaster> countries = new();

            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();
                string query = "SELECT countryid, cname, createdon, updatedon FROM payroll.countrymaster";

                using (var cmd = new NpgsqlCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        countries.Add(new CountryMaster
                        {
                            CountryId = reader.GetInt32(0),
                            CName = reader.GetString(1),
                            CreatedOn = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                            UpdatedOn = reader.IsDBNull(3) ? null : reader.GetDateTime(3)
                        });
                    }
                }
            }

            return Ok(countries);
        }

        // -------------------------------------------------------------
        // 2. GET COUNTRY BY ID
        // -------------------------------------------------------------
        [HttpGet("{id}")]
        public IActionResult GetCountryById(int id)
        {
            CountryMaster? country = null;

            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();
                string query = "SELECT countryid, cname, createdon, updatedon FROM payroll.countrymaster WHERE countryid = @id";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            country = new CountryMaster
                            {
                                CountryId = reader.GetInt32(0),
                                CName = reader.GetString(1),
                                CreatedOn = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                                UpdatedOn = reader.IsDBNull(3) ? null : reader.GetDateTime(3)
                            };
                        }
                    }
                }
            }

            return country == null ? NotFound() : Ok(country);
        }

        // -------------------------------------------------------------
        // 3. CREATE COUNTRY (INSERT)
        // -------------------------------------------------------------
        [HttpPost]
        public IActionResult CreateCountry(CountryMaster model)
        {
            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();
                string query = @"
                    INSERT INTO payroll.countrymaster (cname, createdon)
                    VALUES (@cname, NOW())";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@cname", model.CName ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Country added successfully" });
        }

        // -------------------------------------------------------------
        // 4. UPDATE COUNTRY
        // -------------------------------------------------------------
        [HttpPut("{id}")]
        public IActionResult UpdateCountry(int id, CountryMaster model)
        {
            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();
                string query = @"
                    UPDATE payroll.countrymaster
                    SET cname = @cname, updatedon = NOW()
                    WHERE countryid = @id";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@cname", model.CName ?? (object)DBNull.Value);

                    int rows = cmd.ExecuteNonQuery();

                    if (rows == 0)
                        return NotFound();
                }
            }

            return Ok(new { message = "Country updated successfully" });
        }

        // -------------------------------------------------------------
        // 5. DELETE COUNTRY
        // -------------------------------------------------------------
        [HttpDelete("{id}")]
        public IActionResult DeleteCountry(int id)
        {
            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();
                string query = "DELETE FROM payroll.countrymaster WHERE countryid = @id";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    int rows = cmd.ExecuteNonQuery();

                    if (rows == 0)
                        return NotFound();
                }
            }

            return Ok(new { message = "Country deleted successfully" });
        }
    }
}