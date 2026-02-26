using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Employee;

namespace vaulterpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public EmployeeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ✅ Get all employees (optionally filtered by office ID)
        [HttpGet("byOffice/{officeId}")]
        public IActionResult GetByOffice(int officeId)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                // 1️⃣ Fetch employees
                var empQuery = @"
            SELECT * 
            FROM master.employee_master 
            WHERE office_id = @office_id AND is_active = TRUE";

                var empList = new List<EmployeeDto>();
                using (var empCmd = new NpgsqlCommand(empQuery, conn))
                {
                    empCmd.Parameters.AddWithValue("@office_id", officeId);

                    using var reader = empCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        empList.Add(new EmployeeDto
                        {
                            EmployeeId = reader["employee_id"] as int?,
                            EmployeeCode = reader["employee_code"].ToString(),
                            EmployeeName = reader["employee_name"].ToString(),
                            Email = reader["email"]?.ToString(),
                            PhoneNumber = reader["phone_number"]?.ToString(),
                            OfficeId = reader["office_id"] as int?,
                            Department = reader["department"]?.ToString(),
                            Designation = reader["designation"]?.ToString(),
                            RoleId = reader["role_id"] as int?,
                            ReportsTo = reader["reports_to"] as int?,
                            JoiningDate = reader["joining_date"] as DateTime?,
                            LeavingDate = reader["leaving_date"] as DateTime?,
                            IsActive = (bool)reader["is_active"],
                            ProfileImageUrl = reader["profile_image_url"]?.ToString(),
                            EmploymentType = reader["employement_type"]?.ToString(),
                            DateOfBirth = reader["date_of_birth"] as DateTime?, // ✅ DateTime
                            PanCard = reader["pan_card"]?.ToString(),
                            AadharCard = reader["aadhar_card"]?.ToString(),
                            Address1 = reader["address_1"]?.ToString(),
                            Address2 = reader["address_2"]?.ToString(),
                            City = reader["city"]?.ToString(),
                            State = reader["state"]?.ToString(),
                            Gender = reader["gender"]?.ToString(),
                        });
                    }
                }

                if (!empList.Any())
                    return NotFound(new { message = "No employees found for this office" });

                var empIds = empList.Where(e => e.EmployeeId.HasValue).Select(e => e.EmployeeId.Value).ToList();
                if (empIds.Count == 0)
                    return Ok(empList);

                // 2️⃣ Fetch bank details
                var bankDict = new Dictionary<int, List<BankDetailsDto>>();
                var bankQuery = @"
            SELECT * 
            FROM master.employee_bank_details 
            WHERE employee_id = ANY(@emp_ids) AND is_active = TRUE";

                using (var bankCmd = new NpgsqlCommand(bankQuery, conn))
                {
                    bankCmd.Parameters.AddWithValue("@emp_ids", empIds);

                    using var reader = bankCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        int empId = (int)reader["employee_id"];
                        if (!bankDict.ContainsKey(empId))
                            bankDict[empId] = new List<BankDetailsDto>();

                        bankDict[empId].Add(new BankDetailsDto
                        {
                            BankName = reader["bank_name"]?.ToString(),
                            PanNo = reader["pan_no"]?.ToString(),
                            BankAccNo = reader["bank_acc_no"]?.ToString(),
                            UanNo = reader["uan_no"]?.ToString(),
                            IfscCode = reader["ifsc_code"]?.ToString(),
                            IsActive = (bool)reader["is_active"]
                        });
                    }
                }

                // 3️⃣ Fetch work history
                var workDict = new Dictionary<int, List<WorkHistoryDto>>();
                var workQuery = @"
            SELECT * 
            FROM master.employee_work_history 
            WHERE employee_id = ANY(@emp_ids) AND is_active = TRUE";

                using (var workCmd = new NpgsqlCommand(workQuery, conn))
                {
                    workCmd.Parameters.AddWithValue("@emp_ids", empIds);

                    using var reader = workCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        int empId = (int)reader["employee_id"];
                        if (!workDict.ContainsKey(empId))
                            workDict[empId] = new List<WorkHistoryDto>();

                        workDict[empId].Add(new WorkHistoryDto
                        {
                            CompanyName = reader["company_name"]?.ToString(),
                            Role = reader["role"]?.ToString(),
                            StartDate = reader["start_date"] as DateTime?,
                            EndDate = reader["end_date"] as DateTime?,
                            DateOfJoining = reader["date_of_joining"] as DateTime?,
                            RelievingDate = reader["relieving_date"] as DateTime?,
                            ThirdPartyVerification = (bool)reader["third_party_verification"],
                            IsActive = (bool)reader["is_active"]
                        });
                    }
                }

                // 4️⃣ Attach related data
                foreach (var emp in empList)
                {
                    if (emp.EmployeeId.HasValue)
                    {
                        emp.BankDetails = bankDict.TryGetValue(emp.EmployeeId.Value, out var bankList) ? bankList : new List<BankDetailsDto>();
                        emp.WorkHistory = workDict.TryGetValue(emp.EmployeeId.Value, out var workList) ? workList : new List<WorkHistoryDto>();
                    }
                }

                return Ok(empList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching employees by office", error = ex.Message });
            }
        }

        // ✅ GET employee by ID
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand("SELECT em.*, om.latitude, om.longitude FROM master.employee_master em  LEFT JOIN master.office_master om ON em.office_id = om.office_id  WHERE employee_id = @id AND em.is_active = true", conn);
                cmd.Parameters.AddWithValue("@id", id);

                conn.Open();
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var employee = new EmployeeDto
                    {
                        EmployeeId = (int)reader["employee_id"],
                        EmployeeCode = reader["employee_code"]?.ToString(),
                        EmployeeName = reader["employee_name"]?.ToString(),
                        Email = reader["email"]?.ToString(),
                        PhoneNumber = reader["phone_number"]?.ToString(),
                        OfficeId = (int?)reader["office_id"],
                        Department = reader["department"]?.ToString(),
                        Designation = reader["designation"]?.ToString(),
                        RoleId = (int?)reader["role_id"],
                        ReportsTo = reader["reports_to"] as int?,
                        JoiningDate = reader["joining_date"] as DateTime?,
                        LeavingDate = reader["leaving_date"] as DateTime?,
                        IsActive = (bool)reader["is_active"],
                        ProfileImageUrl = reader["profile_image_url"]?.ToString(),
                        EmploymentType = reader["employement_type"]?.ToString(),
                        DateOfBirth = reader["date_of_birth"] as DateTime?,
                        PanCard = reader["pan_card"]?.ToString(),
                        AadharCard = reader["aadhar_card"]?.ToString(),
                        Address1 = reader["address_1"]?.ToString(),
                        Address2 = reader["address_2"]?.ToString(),
                        City = reader["city"]?.ToString(),
                        State = reader["state"]?.ToString(),
                        Gender = reader["gender"]?.ToString(),
                        Latitude = reader["latitude"]?.ToString(),
                        Longitude = reader["longitude"]?.ToString(),
                    };

                    return Ok(employee);
                }

                return NotFound(new { message = "Employee not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving employee", error = ex.Message });
            }
        }


        // ✅ Create employee
        [HttpPost]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Create([FromForm] EmployeeDto emp)
        {
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();
            await using var tran = await conn.BeginTransactionAsync();

            try
            {
                // 1️⃣ Insert into employee_master
                var insertEmp = @"
            INSERT INTO master.employee_master
            (employee_code, employee_name, email, phone_number, office_id, department, designation, role_id,
             joining_date, is_active, created_by, created_on, employement_type, date_of_birth, pan_card, aadhar_card,
             address_1, address_2, city, state, gender)
            VALUES
            (@EmployeeCode, @EmployeeName, @Email, @PhoneNumber, @OfficeId, @Department, @Designation, @RoleId,
             @JoiningDate, @IsActive, @CreatedBy, NOW(), @EmploymentType, @DateOfBirth, @PanCard, @AadharCard,
             @Address1, @Address2, @City, @State, @Gender)
            RETURNING employee_id";

                await using var cmdEmp = new NpgsqlCommand(insertEmp, conn, tran);
                cmdEmp.Parameters.AddWithValue("@EmployeeCode", emp.EmployeeCode ?? "");
                cmdEmp.Parameters.AddWithValue("@EmployeeName", emp.EmployeeName ?? "");
                cmdEmp.Parameters.AddWithValue("@Email", emp.Email ?? "");
                cmdEmp.Parameters.AddWithValue("@PhoneNumber", emp.PhoneNumber ?? "");
                cmdEmp.Parameters.AddWithValue("@OfficeId", emp.OfficeId ?? (object)DBNull.Value);
                cmdEmp.Parameters.AddWithValue("@Department", emp.Department ?? "");
                cmdEmp.Parameters.AddWithValue("@Designation", emp.Designation ?? "");
                cmdEmp.Parameters.AddWithValue("@RoleId", emp.RoleId ?? (object)DBNull.Value);
                cmdEmp.Parameters.AddWithValue("@JoiningDate", emp.JoiningDate ?? (object)DBNull.Value);
                cmdEmp.Parameters.AddWithValue("@IsActive", true);
                cmdEmp.Parameters.AddWithValue("@CreatedBy", emp.CreatedBy ?? 1);
                cmdEmp.Parameters.AddWithValue("@EmploymentType", emp.EmploymentType ?? "");
                cmdEmp.Parameters.AddWithValue("@DateOfBirth", emp.DateOfBirth ?? (object)DBNull.Value);
                cmdEmp.Parameters.AddWithValue("@PanCard", emp.PanCard ?? "");
                cmdEmp.Parameters.AddWithValue("@AadharCard", emp.AadharCard ?? "");
                cmdEmp.Parameters.AddWithValue("@Address1", emp.Address1 ?? "");
                cmdEmp.Parameters.AddWithValue("@Address2", emp.Address2 ?? "");
                cmdEmp.Parameters.AddWithValue("@City", emp.City ?? "");
                cmdEmp.Parameters.AddWithValue("@State", emp.State ?? "");
                cmdEmp.Parameters.AddWithValue("@Gender", emp.Gender ?? "");

                var employeeId = (int)await cmdEmp.ExecuteScalarAsync();

                // 2️⃣ Insert WorkHistory
                if (emp.WorkHistory != null)
                {
                    foreach (var wh in emp.WorkHistory)
                    {
                        // Save resume(s) to disk
                        var resumePaths = new List<string>();
                        if (wh.ResumeFiles != null)
                        {
                            foreach (var file in wh.ResumeFiles)
                            {
                                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                                var path = Path.Combine("wwwroot/resumes", fileName);
                                await using var stream = new FileStream(path, FileMode.Create);
                                await file.CopyToAsync(stream);
                                resumePaths.Add(fileName);
                            }
                        }

                        var insertWh = @"
                    INSERT INTO master.employee_work_history
                    (employee_id, company_name, role, start_date, end_date, date_of_joining, relieving_date,
                     third_party_verification, office_id, resume)
                    VALUES
                    (@EmployeeId, @Company, @Role, @StartDate, @EndDate, @DOJ, @Relieve, @TPV, @OfficeId, @Resume)";

                        await using var cmdWh = new NpgsqlCommand(insertWh, conn, tran);
                        cmdWh.Parameters.AddWithValue("@EmployeeId", employeeId);
                        cmdWh.Parameters.AddWithValue("@Company", wh.CompanyName ?? "");
                        cmdWh.Parameters.AddWithValue("@Role", wh.Role ?? "");
                        cmdWh.Parameters.AddWithValue("@StartDate", wh.StartDate ?? (object)DBNull.Value);
                        cmdWh.Parameters.AddWithValue("@EndDate", wh.EndDate ?? (object)DBNull.Value);
                        cmdWh.Parameters.AddWithValue("@DOJ", wh.DateOfJoining ?? (object)DBNull.Value);
                        cmdWh.Parameters.AddWithValue("@Relieve", wh.RelievingDate ?? (object)DBNull.Value);
                        cmdWh.Parameters.AddWithValue("@TPV", wh.ThirdPartyVerification);
                        cmdWh.Parameters.AddWithValue("@OfficeId", wh.OfficeId ?? (object)DBNull.Value);
                        cmdWh.Parameters.AddWithValue("@Resume", resumePaths.ToArray());
                        await cmdWh.ExecuteNonQueryAsync();
                    }
                }

                // 3️⃣ Insert BankDetails
                if (emp.BankDetails != null)
                {
                    foreach (var bd in emp.BankDetails)
                    {
                        var insertBank = @"
                    INSERT INTO master.employee_bank_details
                    (employee_id, bank_name, pan_no, bank_acc_no, uan_no, ifsc_code, office_id, created_on, created_by, is_active)
                    VALUES
                    (@EmployeeId, @BankName, @PanNo, @BankAccNo, @UanNo, @IfscCode, @OfficeId, NOW(), @CreatedBy, TRUE)";

                        await using var cmdBank = new NpgsqlCommand(insertBank, conn, tran);
                        cmdBank.Parameters.AddWithValue("@EmployeeId", employeeId);
                        cmdBank.Parameters.AddWithValue("@BankName", bd.BankName ?? "");
                        cmdBank.Parameters.AddWithValue("@PanNo", bd.PanNo ?? "");
                        cmdBank.Parameters.AddWithValue("@BankAccNo", bd.BankAccNo ?? "");
                        cmdBank.Parameters.AddWithValue("@UanNo", bd.UanNo ?? "");
                        cmdBank.Parameters.AddWithValue("@IfscCode", bd.IfscCode ?? "");
                        cmdBank.Parameters.AddWithValue("@OfficeId", emp.OfficeId ?? (object)DBNull.Value);
                        cmdBank.Parameters.AddWithValue("@CreatedBy", emp.CreatedBy ?? 1);
                        await cmdBank.ExecuteNonQueryAsync();
                    }
                }

                await tran.CommitAsync();
                return Ok(new { message = "Employee, WorkHistory, and BankDetails created successfully", employeeId });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return StatusCode(500, new { message = "Error creating employee", error = ex.Message });
            }
        }



        // --------------------------- UPDATE ---------------------------
        [HttpPut("{id}")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Update(int id, [FromForm] EmployeeDto emp)
        {
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();
            await using var tran = await conn.BeginTransactionAsync();

            try
            {
                // 1️⃣ Update employee_master
                var updateEmp = @"
            UPDATE master.employee_master SET
                employee_name=@Name, email=@Email, phone_number=@Phone,
                department=@Dept, designation=@Designation, role_id=@RoleId,
                reports_to=@ReportsTo, joining_date=@JoiningDate, leaving_date=@LeavingDate,
                modified_on=NOW(),
                employement_type=@EmpType, date_of_birth=@DOB, pan_card=@Pan,
                aadhar_card=@Aadhar, address_1=@Addr1, address_2=@Addr2,
                city=@City, state=@State, gender=@Gender
            WHERE employee_id=@Id";

                await using var cmd = new NpgsqlCommand(updateEmp, conn, tran);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Name", emp.EmployeeName ?? "");
                cmd.Parameters.AddWithValue("@Email", emp.Email ?? "");
                cmd.Parameters.AddWithValue("@Phone", emp.PhoneNumber ?? "");
                cmd.Parameters.AddWithValue("@Dept", emp.Department ?? "");
                cmd.Parameters.AddWithValue("@Designation", emp.Designation ?? "");
                cmd.Parameters.AddWithValue("@RoleId", emp.RoleId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ReportsTo", emp.ReportsTo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@JoiningDate", emp.JoiningDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@LeavingDate", emp.LeavingDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EmpType", emp.EmploymentType ?? "");
                cmd.Parameters.AddWithValue("@DOB", emp.DateOfBirth ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Pan", emp.PanCard ?? "");
                cmd.Parameters.AddWithValue("@Aadhar", emp.AadharCard ?? "");
                cmd.Parameters.AddWithValue("@Addr1", emp.Address1 ?? "");
                cmd.Parameters.AddWithValue("@Addr2", emp.Address2 ?? "");
                cmd.Parameters.AddWithValue("@City", emp.City ?? "");
                cmd.Parameters.AddWithValue("@State", emp.State ?? "");
                cmd.Parameters.AddWithValue("@Gender", emp.Gender ?? "");
                await cmd.ExecuteNonQueryAsync();

                // 2️⃣ Update BankDetails
                if (emp.BankDetails != null && emp.BankDetails.Count > 0)
                {
                    foreach (var bank in emp.BankDetails)
                    {
                        var updateBank = @"
                    UPDATE master.employee_bank_details SET
                        bank_name=@Bank, pan_no=@Pan, bank_acc_no=@Acc, uan_no=@Uan, ifsc_code=@Ifsc,
                        office_id=@Office, updated_on=NOW()
                    WHERE employee_id=@EmpId";

                        await using var cmdBank = new NpgsqlCommand(updateBank, conn, tran);
                        cmdBank.Parameters.AddWithValue("@EmpId", id);
                        cmdBank.Parameters.AddWithValue("@Bank", bank.BankName ?? "");
                        cmdBank.Parameters.AddWithValue("@Pan", bank.PanNo ?? "");
                        cmdBank.Parameters.AddWithValue("@Acc", bank.BankAccNo ?? "");
                        cmdBank.Parameters.AddWithValue("@Uan", bank.UanNo ?? "");
                        cmdBank.Parameters.AddWithValue("@Ifsc", bank.IfscCode ?? "");
                        cmdBank.Parameters.AddWithValue("@Office", emp.OfficeId ?? (object)DBNull.Value);
                        await cmdBank.ExecuteNonQueryAsync();
                    }
                }

                // 3️⃣ Refresh WorkHistory (delete + insert new)
                if (emp.WorkHistory != null && emp.WorkHistory.Count > 0)
                {
                    var deleteWork = "DELETE FROM master.employee_work_history WHERE employee_id=@EmpId";
                    await using (var delCmd = new NpgsqlCommand(deleteWork, conn, tran))
                    {
                        delCmd.Parameters.AddWithValue("@EmpId", id);
                        await delCmd.ExecuteNonQueryAsync();
                    }

                    foreach (var wh in emp.WorkHistory)
                    {
                        byte[] resumeBytes = null;
                        if (wh.ResumeFiles != null)
                        {
                            foreach (var file in wh.ResumeFiles)
                            {
                                await using var ms = new MemoryStream();
                                await file.CopyToAsync(ms);
                                resumeBytes = ms.ToArray();
                            }
                        }

                        var insertWork = @"
                    INSERT INTO master.employee_work_history
                    (employee_id, resume, company_name, role, start_date, end_date,
                     date_of_joining, relieving_date, third_party_verification, office_id, created_on)
                    VALUES
                    (@EmpId, @Resume, @Company, @Role, @Start, @End, @DOJ, @Relieve, @TPV, @Office, NOW())";

                        await using var cmdWh = new NpgsqlCommand(insertWork, conn, tran);
                        cmdWh.Parameters.AddWithValue("@EmpId", id);
                        cmdWh.Parameters.AddWithValue("@Resume", (object)resumeBytes ?? DBNull.Value);
                        cmdWh.Parameters.AddWithValue("@Company", wh.CompanyName ?? "");
                        cmdWh.Parameters.AddWithValue("@Role", wh.Role ?? "");
                        cmdWh.Parameters.AddWithValue("@Start", wh.StartDate ?? (object)DBNull.Value);
                        cmdWh.Parameters.AddWithValue("@End", wh.EndDate ?? (object)DBNull.Value);
                        cmdWh.Parameters.AddWithValue("@DOJ", wh.DateOfJoining ?? (object)DBNull.Value);
                        cmdWh.Parameters.AddWithValue("@Relieve", wh.RelievingDate ?? (object)DBNull.Value);
                        cmdWh.Parameters.AddWithValue("@TPV", wh.ThirdPartyVerification);
                        cmdWh.Parameters.AddWithValue("@Office", emp.OfficeId ?? (object)DBNull.Value);
                        await cmdWh.ExecuteNonQueryAsync();
                    }
                }

                await tran.CommitAsync();
                return Ok(new { message = "Employee updated successfully", status = true });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return StatusCode(500, new { message = "Error updating employee", error = ex.Message });
            }
        }


        // Soft delete employee optionally from specific tables
        [HttpDelete("{id}")]
        public IActionResult SoftDelete(int id, [FromQuery] string table = "all")
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();
                using var tran = conn.BeginTransaction();

                // Helper dictionary to map table names to SQL
                var tableMap = new Dictionary<string, string>
        {
            { "master", "UPDATE master.employee_master SET is_active = FALSE WHERE employee_id = @id" },
            { "bank", "UPDATE master.employee_bank_details SET is_active = FALSE WHERE employee_id = @id" },
            { "workhistory", "UPDATE master.employee_work_history SET is_active = FALSE WHERE employee_id = @id" }
        };

                if (table.ToLower() == "all")
                {
                    foreach (var sql in tableMap.Values)
                    {
                        using var cmd = new NpgsqlCommand(sql, conn, tran);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    if (!tableMap.ContainsKey(table.ToLower()))
                        return BadRequest(new { message = "Invalid table name. Use 'master', 'bank', 'workhistory', or 'all'." });

                    using var cmd = new NpgsqlCommand(tableMap[table.ToLower()], conn, tran);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                tran.Commit();
                return Ok(new { message = $"Employee soft deleted from '{table}' successfully", status = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error performing soft delete", error = ex.Message });
            }
        }

        [HttpGet("getBy/{imageName}")]
        public IActionResult GetByImage(string imageName)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand(@"
            SELECT 
                em.*, 
                om.office_name, 
                om.latitude, 
                om.longitude,
                u.username 
            FROM master.employee_master em 
            LEFT JOIN master.office_master om ON em.office_id = om.office_id 
            LEFT JOIN identity.user u ON u.employee_id = em.employee_id 
            WHERE em.profile_image_name = @imageName  
              AND em.is_active = true;", conn);

                cmd.Parameters.AddWithValue("@imageName", imageName);

                conn.Open();
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string baseUrl = "http://43.230.64.37:8000/images/";
                    string profileImageName = reader["profile_image_name"] != DBNull.Value ? reader["profile_image_name"].ToString() : null;
                    string profileImageUrl = profileImageName != null ? baseUrl + profileImageName : null;

                    var employee = new EmployeeDto
                    {
                        EmployeeId = (int)reader["employee_id"],
                        EmployeeCode = reader["employee_code"]?.ToString(),
                        EmployeeName = reader["employee_name"]?.ToString(),
                        Email = reader["email"]?.ToString(),
                        PhoneNumber = reader["phone_number"]?.ToString(),
                        OfficeId = (int?)reader["office_id"],
                        Department = reader["department"]?.ToString(),
                        Designation = reader["designation"]?.ToString(),
                        RoleId = (int?)reader["role_id"],
                        ReportsTo = reader["reports_to"] as int?,
                        JoiningDate = reader["joining_date"] as DateTime?,
                        LeavingDate = reader["leaving_date"] as DateTime?,
                        IsActive = (bool)reader["is_active"],
                        ProfileImageUrl = profileImageUrl,
                        EmploymentType = reader["employement_type"]?.ToString(),
                        DateOfBirth = reader["date_of_birth"] as DateTime?,
                        PanCard = reader["pan_card"]?.ToString(),
                        AadharCard = reader["aadhar_card"]?.ToString(),
                        Address1 = reader["address_1"]?.ToString(),
                        Address2 = reader["address_2"]?.ToString(),
                        City = reader["city"]?.ToString(),
                        State = reader["state"]?.ToString(),
                        Gender = reader["gender"]?.ToString(),
                        OfficeName = reader["office_name"]?.ToString(),
                        Latitude = reader["latitude"]?.ToString(),
                        Longitude = reader["longitude"]?.ToString(),
                        Username = reader["username"]?.ToString() // ✅ Add this line
                    };

                    return Ok(employee);
                }

                return NotFound(new { message = "Employee not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving employee", error = ex.Message });
            }
        }

    }
}
