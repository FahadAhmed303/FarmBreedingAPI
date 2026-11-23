using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using FarmBreedingAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FarmBreedingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public partial class AnimalController : ControllerBase
    {
        private readonly string connectionString =
            "Host=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.lnndywzphqtvzcvunmqc;Password=qAZVexd1DM2Ya2UE;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;Timeout=15;Command Timeout=30";


        // ============================
        // 🔥 COMMON DATE FIX METHOD
        // ============================
        private DateTime? SafeDate(object val)
        {
            if (val == null || val == DBNull.Value)
                return null;

            if (val is DateTime dt)
                return dt;

            if (val is DateOnly d)
                return d.ToDateTime(TimeOnly.MinValue);

            return null;
        }

        // ============================
        // 1. FETCH ANIMAL
        // ============================
        [HttpGet("{atcode}")]
        public async Task<IActionResult> GetAnimal(string atcode)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                SELECT ""ATCode"", ""gender"", ""ATCategoryCode"", ""DOB"",
       sourcetype, purchasedate, price, agentname, mothercode,
       ""AnimalNo"", ""Breed"", ""MotherNumber"",
       COALESCE(""GoatStatus"", 'Active') AS ""GoatStatus"",
       ""SoldExpiredDate"", ""SellingAmount"", ""ExpiredRemark""
FROM ""ArticleInfo01"" 
WHERE ""ATCode"" = @ATCode";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ATCode", atcode);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return NotFound("Animal not found");

                return Ok(new
                {
                    atCode = reader["ATCode"]?.ToString(),
                    gender = reader["gender"]?.ToString(),
                    atCategoryCode = reader["ATCategoryCode"]?.ToString(),
                    dob = SafeDate(reader["DOB"]),

                    sourceType = reader["sourcetype"] == DBNull.Value
                        ? null
                        : reader["sourcetype"].ToString(),

                    purchaseDate = SafeDate(reader["purchasedate"]),

                    price = reader["price"] == DBNull.Value
                        ? null
                        : (decimal?)Convert.ToDecimal(reader["price"]),

                    agentName = reader["agentname"] == DBNull.Value
                        ? null
                        : reader["agentname"].ToString(),

                    motherCode = reader["mothercode"] == DBNull.Value
                        ? null
                        : reader["mothercode"].ToString(),

                    animalNo = reader["AnimalNo"] == DBNull.Value
                        ? null
                        : (int?)Convert.ToInt32(reader["AnimalNo"]),

                    breed = reader["Breed"] == DBNull.Value
                        ? null
                        : reader["Breed"].ToString(),

                    motherNumber = reader["MotherNumber"] == DBNull.Value
                        ? null
                        : (int?)Convert.ToInt32(reader["MotherNumber"]),

                    goatStatus = reader["GoatStatus"] == DBNull.Value ? "Active" : reader["GoatStatus"].ToString(),
                    soldExpiredDate = SafeDate(reader["SoldExpiredDate"]),
                    sellingAmount = reader["SellingAmount"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["SellingAmount"]),
                    expiredRemark = reader["ExpiredRemark"] == DBNull.Value ? null : reader["ExpiredRemark"].ToString()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================
        // 2. FETCH ATCODE BY ANIMAL NO
        // ============================
        [HttpGet("byanimalno/{animalno}")]
        public async Task<IActionResult> GetAnimalByAnimalNo(int animalno)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                SELECT ""ATCode""
                FROM ""ArticleInfo01""
                WHERE ""AnimalNo"" = @AnimalNo";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AnimalNo", animalno);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                    return NotFound("Animal No not found");

                return Ok(new
                {
                    atCode = result.ToString()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================
        // FETCH ANIMAL NO BY ATCODE
        // ============================
        [HttpGet("byatcode/{atcode}")]
        public async Task<IActionResult> GetAnimalNoByATCode(string atcode)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
        SELECT ""AnimalNo""
        FROM ""ArticleInfo01""
        WHERE ""ATCode"" = @ATCode";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ATCode", atcode);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                    return NotFound("Animal No not found");

                return Ok(new
                {
                    animalNo = Convert.ToInt32(result)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================
        // 3. FETCH ALL ANIMAL NOS
        // ============================
        [HttpGet("animalnos")]
        public async Task<IActionResult> GetAnimalNos()
        {
            try
            {
                var list = new List<int>();

                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                SELECT ""AnimalNo""
                FROM ""ArticleInfo01""
                WHERE ""AnimalNo"" IS NOT NULL
                  AND COALESCE(""GoatStatus"", 'Active') = 'Active'
                ORDER BY ""AnimalNo""";

                await using var cmd = new NpgsqlCommand(sql, conn);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(Convert.ToInt32(reader["AnimalNo"]));
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================
        // 2. FETCH GROWTH (FIXED)
        // ============================
        [HttpGet("growth/{atcode}")]
        public async Task<IActionResult> GetGrowth(string atcode)
        {
            try
            {
                var list = new List<object>();

                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                SELECT ""ATCode"", ""Weight"", ""Height"", ""Width"", ""RecordDate"" 
                FROM ""ArticleInfo03"" 
                WHERE ""ATCode""=@ATCode 
                ORDER BY ""RecordDate"" DESC";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ATCode", atcode);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            atCode = reader["ATCode"].ToString(),

                            weight = reader["Weight"] == DBNull.Value
                                ? 0
                                : Convert.ToDouble(reader["Weight"]),

                            height = reader["Height"] == DBNull.Value
                                ? 0
                                : Convert.ToDouble(reader["Height"]),

                            width = reader["Width"] == DBNull.Value
                                ? 0
                                : Convert.ToDouble(reader["Width"]),

                            recordDate = SafeDate(reader["RecordDate"])
                        });
                    }
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================
        // 3. FETCH PHOTOS
        // ============================
        [HttpGet("photos/{atcode}")]
        public async Task<IActionResult> GetPhotos(string atcode)
        {
            try
            {
                var photos = new List<string>();

                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"SELECT photo01, photo02, photo03 
                               FROM ""ArticleInfo02"" 
                               WHERE ""ATCode""=@ATCode";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ATCode", atcode);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader["photo01"] != DBNull.Value)
                            photos.Add(reader["photo01"].ToString());

                        if (reader["photo02"] != DBNull.Value)
                            photos.Add(reader["photo02"].ToString());

                        if (reader["photo03"] != DBNull.Value)
                            photos.Add(reader["photo03"].ToString());
                    }
                }

                return Ok(photos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================
        // 4. SAVE ANIMAL
        // ============================
        [HttpPost("save")]
        public async Task<IActionResult> SaveAnimal([FromBody] Animal model)
        {
            try
            {
                if (model.SourceType == "PURCHASE")
                {
                    model.MotherCode = null;
                    model.MotherNumber = null;
                }
                else if (model.SourceType == "BORN")
                {
                    model.PurchaseDate = null;
                    model.Price = null;
                    model.AgentName = null;
                }

                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                UPDATE ""ArticleInfo01""
                SET ""gender"" = @gender,
                    ""ATCategoryCode"" = @ATCategoryCode,
                    ""DOB"" = @DOB,
                    sourcetype = @SourceType,
                    purchasedate = @PurchaseDate,
                    price = @Price,
                    agentname = @AgentName,
                    mothercode = @MotherCode,
                    ""MotherNumber"" = @MotherNumber,
                    ""Breed"" = @Breed
                WHERE ""ATCode"" = @ATCode";

                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue(
                    "@ATCode",
                    model.ATCode ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@gender",
                    model.gender ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@ATCategoryCode",
                    model.ATCategoryCode ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@DOB",
                    model.DOB ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@PurchaseDate",
                    model.PurchaseDate ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@Price",
                    model.Price.HasValue
                        ? model.Price.Value
                        : (object)DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@MotherCode",
                    string.IsNullOrEmpty(model.MotherCode)
                        ? (object)DBNull.Value
                        : model.MotherCode);

                cmd.Parameters.AddWithValue(
                    "@MotherNumber",
                    model.MotherNumber.HasValue
                        ? model.MotherNumber.Value
                        : (object)DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@Breed",
                    (object?)model.Breed ?? DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@AgentName",
                    string.IsNullOrEmpty(model.AgentName)
                        ? (object)DBNull.Value
                        : model.AgentName);

                cmd.Parameters.AddWithValue(
                    "@SourceType",
                    string.IsNullOrEmpty(model.SourceType)
                        ? (object)DBNull.Value
                        : model.SourceType);

                int rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    return BadRequest("UPDATE FAILED");

                return Ok(new
                {
                    message = "Updated successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================
        // UPDATE GOAT STATUS (ACTIVE / SOLD / EXPIRED)
        // ============================
        public class GoatStatusUpdateModel
        {
            public string? ATCode { get; set; }
            public string? GoatStatus { get; set; }
            public DateTime? SoldExpiredDate { get; set; }
            public decimal? SellingAmount { get; set; }
            public string? ExpiredRemark { get; set; }
        }

        [HttpPost("status/update")]
        public async Task<IActionResult> UpdateGoatStatus([FromBody] GoatStatusUpdateModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.ATCode)) return BadRequest("ATCode is required.");
            var status = string.IsNullOrWhiteSpace(model.GoatStatus) ? "Active" : model.GoatStatus.Trim();
            if (status != "Active" && status != "Sold" && status != "Expired") return BadRequest("Invalid Goat Status.");
            if (status != "Active" && !model.SoldExpiredDate.HasValue) return BadRequest("Sold / Expired Date is required.");
            if (status == "Sold" && model.SellingAmount.HasValue && model.SellingAmount.Value < 0) return BadRequest("Selling Amount cannot be negative.");
            if (status != "Sold" && model.SellingAmount.HasValue) return BadRequest("Selling Amount is only allowed for Sold goats.");
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                const string sql = @"UPDATE ""ArticleInfo01"" SET ""GoatStatus""=@Status, ""SoldExpiredDate""=@Date, ""SellingAmount""=@Amount, ""ExpiredRemark""=@Remark WHERE ""ATCode""=@ATCode";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Date", NpgsqlDbType.Date, status == "Active" ? DBNull.Value : model.SoldExpiredDate!.Value.Date);
                cmd.Parameters.AddWithValue("@Amount", NpgsqlDbType.Numeric, status == "Sold" && model.SellingAmount.HasValue ? (object)model.SellingAmount.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Remark", NpgsqlDbType.Text, status == "Expired" && !string.IsNullOrWhiteSpace(model.ExpiredRemark) ? model.ExpiredRemark.Trim() : DBNull.Value);
                cmd.Parameters.AddWithValue("@ATCode", model.ATCode);
                if (await cmd.ExecuteNonQueryAsync() == 0) return NotFound("Goat not found.");
                return Ok(new { message = "Goat status updated successfully." });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }


        // Department reassignment: append a new effective-dated assignment.
        // Active departments for the Department Edit dropdown.
        [HttpGet("department/list")]
        public async Task<IActionResult> GetActiveDepartments()
        {
            try
            {
                var list = new List<object>();
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                const string sql = @"
                    SELECT ""DepartmentID"", ""DepartmentName""
                    FROM public.""Department""
                    WHERE ""IsActive"" = TRUE
                    ORDER BY ""DepartmentID""";
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        departmentID = Convert.ToInt32(reader["DepartmentID"]),
                        departmentName = reader["DepartmentName"].ToString()
                    });
                }
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Active goat list for Department Edit, including identifying details and latest department.
        [HttpGet("department/goats")]
        public async Task<IActionResult> GetDepartmentGoats()
        {
            try
            {
                var list = new List<object>();
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                const string sql = @"
                    SELECT
                        a.""AnimalNo"",
                        a.""gender"",
                        a.""Breed"",
                        d.""DepartmentID"" AS ""CurrentDepartmentID""
                    FROM ""ArticleInfo01"" a
                    LEFT JOIN LATERAL
                    (
                        SELECT ""DepartmentID""
                        FROM ""AnimalDepartment""
                        WHERE ""ATCode"" = a.""ATCode""
                        ORDER BY ""EffectiveDate"" DESC, ""AssignmentID"" DESC
                        LIMIT 1
                    ) d ON TRUE
                    WHERE a.""AnimalNo"" IS NOT NULL
                      AND COALESCE(a.""GoatStatus"", 'Active') = 'Active'
                    ORDER BY a.""AnimalNo""";

                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        animalNo = Convert.ToInt32(reader["AnimalNo"]),
                        gender = reader["gender"] == DBNull.Value ? null : reader["gender"].ToString(),
                        breed = reader["Breed"] == DBNull.Value ? null : reader["Breed"].ToString(),
                        currentDepartmentID = reader["CurrentDepartmentID"] == DBNull.Value
                            ? (int?)null
                            : Convert.ToInt32(reader["CurrentDepartmentID"])
                    });
                }
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public class DepartmentAssignmentModel
        {
            public int AnimalNo { get; set; }
            public int DepartmentID { get; set; }
            public DateTime EffectiveDate { get; set; }
        }

        public class BulkDepartmentAssignmentModel
        {
            public List<int> AnimalNos { get; set; } = new List<int>();
            public int DepartmentID { get; set; }
            public DateTime EffectiveDate { get; set; }
        }

        // Assign one department/date to multiple selected active goats.
        // All assignments are inserted together; previous department history is retained.
        [HttpPost("department/assign-bulk")]
        public async Task<IActionResult> AssignDepartmentBulk([FromBody] BulkDepartmentAssignmentModel model)
        {
            if (model == null || model.AnimalNos == null || model.AnimalNos.Count == 0)
                return BadRequest("Select at least one goat.");
            if (model.DepartmentID <= 0)
                return BadRequest("Department ID must be greater than zero.");
            if (model.EffectiveDate == default)
                return BadRequest("Effective Date is required.");
            if (model.AnimalNos.Any(n => n <= 0))
                return BadRequest("All Animal Numbers must be greater than zero.");

            var animalNos = new List<int>(new HashSet<int>(model.AnimalNos));

            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                await using var transaction = await conn.BeginTransactionAsync();

                const string validateDepartmentSql = @"
                    SELECT COUNT(*)
                    FROM public.""Department""
                    WHERE ""DepartmentID"" = @DepartmentID
                      AND ""IsActive"" = TRUE";
                await using (var departmentCmd = new NpgsqlCommand(validateDepartmentSql, conn, transaction))
                {
                    departmentCmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                    var departmentExists = Convert.ToInt32(await departmentCmd.ExecuteScalarAsync());
                    if (departmentExists == 0)
                        return BadRequest("Selected department does not exist or is inactive.");
                }

                const string validateSql = @"
                    SELECT COUNT(*)
                    FROM ""ArticleInfo01""
                    WHERE ""AnimalNo"" = ANY(@AnimalNos)
                      AND COALESCE(""GoatStatus"", 'Active') = 'Active'";

                int foundCount;
                await using (var validateCmd = new NpgsqlCommand(validateSql, conn, transaction))
                {
                    validateCmd.Parameters.AddWithValue("@AnimalNos", animalNos.ToArray());
                    foundCount = Convert.ToInt32(await validateCmd.ExecuteScalarAsync());
                }

                if (foundCount != animalNos.Count)
                    return BadRequest("One or more selected Animal Numbers were not found or are not active. No assignments were saved.");

                // Do not add a redundant history row when the goat is already
                // in this department as of the requested EffectiveDate.
                const string insertSql = @"
                    INSERT INTO ""AnimalDepartment""
                        (""ATCode"", ""AnimalNo"", ""DepartmentID"", ""EffectiveDate"")
                    SELECT a.""ATCode"", a.""AnimalNo"", @DepartmentID, @EffectiveDate
                    FROM ""ArticleInfo01"" a
                    WHERE a.""AnimalNo"" = ANY(@AnimalNos)
                      AND COALESCE(a.""GoatStatus"", 'Active') = 'Active'
                      AND COALESCE(
                          (
                              SELECT d.""DepartmentID""
                              FROM ""AnimalDepartment"" d
                              WHERE d.""ATCode"" = a.""ATCode""
                                AND d.""EffectiveDate"" <= @EffectiveDate
                              ORDER BY d.""EffectiveDate"" DESC, d.""AssignmentID"" DESC
                              LIMIT 1
                          ), -1
                      ) <> @DepartmentID";

                int insertedCount;
                await using (var insertCmd = new NpgsqlCommand(insertSql, conn, transaction))
                {
                    insertCmd.Parameters.AddWithValue("@AnimalNos", animalNos.ToArray());
                    insertCmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                    insertCmd.Parameters.AddWithValue("@EffectiveDate", NpgsqlDbType.Date, model.EffectiveDate.Date);
                    insertedCount = await insertCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return Ok(new
                {
                    message = insertedCount == 0
                        ? "No new assignments were needed. All selected goats were already in this department for the selected effective date."
                        : "Department assignments processed successfully.",
                    assignedCount = insertedCount,
                    skippedAlreadyInDepartment = animalNos.Count - insertedCount,
                    departmentID = model.DepartmentID,
                    effectiveDate = model.EffectiveDate.Date
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("department/assign")]
        public async Task<IActionResult> AssignDepartment([FromBody] DepartmentAssignmentModel model)
        {
            if (model == null || model.AnimalNo <= 0 || model.DepartmentID <= 0) return BadRequest("Animal No and Department ID are required.");
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                const string sql = @"
                    INSERT INTO ""AnimalDepartment""
                        (""ATCode"", ""AnimalNo"", ""DepartmentID"", ""EffectiveDate"")
                    SELECT ""ATCode"", ""AnimalNo"", @DepartmentID, @EffectiveDate
                    FROM ""ArticleInfo01""
                    WHERE ""AnimalNo"" = @AnimalNo";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AnimalNo", model.AnimalNo);
                cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                cmd.Parameters.AddWithValue("@EffectiveDate", NpgsqlDbType.Date, model.EffectiveDate.Date);
                var count = await cmd.ExecuteNonQueryAsync();
                if (count == 0) return NotFound("Animal No not found.");
                return Ok(new { message = "Department assignment saved." });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // ============================
        // 5. SAVE GROWTH
        // ============================
        [HttpPost("growth/save")]
        public async Task<IActionResult> SaveGrowth([FromBody] GrowthRecord model)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                INSERT INTO ""ArticleInfo03""
                (""ATCode"", ""RecordDate"", ""Weight"", ""Height"", ""Width"")
                VALUES
                (@ATCode, @RecordDate, @Weight, @Height, @Width)";

                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@ATCode", model.ATCode);
                cmd.Parameters.AddWithValue("@RecordDate", model.RecordDate);
                cmd.Parameters.AddWithValue("@Weight", model.Weight);
                cmd.Parameters.AddWithValue("@Height", model.Height);
                cmd.Parameters.AddWithValue("@Width", model.Width);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    message = "Growth saved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================
        // 6. ANIMAL REPORT
        // ============================
        [HttpGet("report")]
        public async Task<IActionResult> GetAnimalReport()
        {
            try
            {
                var list = new List<object>();

                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT
                        a.""AnimalNo"",
                        a.""ATCode"",
                        a.""gender"",
                        a.""DOB"",
                        a.""Breed"",
                        a.sourcetype,
                        a.purchasedate,
                        a.price,
                        a.agentname,
                        a.""MotherNumber"",
                        a.mothercode,

                        g.""RecordDate"",
                        g.""Weight"",
                        g.""Height"",
                        g.""Width"",

                        d.""DepartmentID"" AS ""DepartmentNo""

                    FROM ""ArticleInfo01"" a

                    LEFT JOIN LATERAL
                    (
                        SELECT
                            ""RecordDate"",
                            ""Weight"",
                            ""Height"",
                            ""Width""
                        FROM ""ArticleInfo03""
                        WHERE ""ATCode"" = a.""ATCode""
                        ORDER BY ""RecordDate"" DESC, recordid DESC
                        LIMIT 1
                    ) g ON true

                    LEFT JOIN LATERAL
                    (
                        SELECT
                            ""DepartmentID""
                        FROM ""AnimalDepartment""
                        WHERE ""ATCode"" = a.""ATCode""
                        ORDER BY ""EffectiveDate"" DESC, ""AssignmentID"" DESC
                        LIMIT 1
                    ) d ON true

                    WHERE COALESCE(a.""GoatStatus"", 'Active') = 'Active'
                    ORDER BY a.""AnimalNo""";

                await using var cmd = new NpgsqlCommand(sql, conn);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        // ============================
                        // ORIGINAL 11 FIELDS
                        // ============================

                        animalNo = reader["AnimalNo"] == DBNull.Value
                            ? null
                            : (int?)Convert.ToInt32(reader["AnimalNo"]),

                        atCode = reader["ATCode"] == DBNull.Value
                            ? null
                            : reader["ATCode"].ToString(),

                        gender = reader["gender"] == DBNull.Value
                            ? null
                            : reader["gender"].ToString(),

                        dob = SafeDate(reader["DOB"]),

                        breed = reader["Breed"] == DBNull.Value
                            ? null
                            : reader["Breed"].ToString(),

                        sourceType = reader["sourcetype"] == DBNull.Value
                            ? null
                            : reader["sourcetype"].ToString(),

                        purchaseDate = SafeDate(reader["purchasedate"]),

                        price = reader["price"] == DBNull.Value
                            ? null
                            : (decimal?)Convert.ToDecimal(reader["price"]),

                        agentName = reader["agentname"] == DBNull.Value
                            ? null
                            : reader["agentname"].ToString(),

                        motherNumber = reader["MotherNumber"] == DBNull.Value
                            ? null
                            : (int?)Convert.ToInt32(reader["MotherNumber"]),

                        motherCode = reader["mothercode"] == DBNull.Value
                            ? null
                            : reader["mothercode"].ToString(),

                        // ============================
                        // LATEST GROWTH
                        // ============================

                        recordDate = SafeDate(reader["RecordDate"]),

                        weight = reader["Weight"] == DBNull.Value
                            ? null
                            : (decimal?)Convert.ToDecimal(reader["Weight"]),

                        height = reader["Height"] == DBNull.Value
                            ? null
                            : (decimal?)Convert.ToDecimal(reader["Height"]),

                        width = reader["Width"] == DBNull.Value
                            ? null
                            : (decimal?)Convert.ToDecimal(reader["Width"]),

                        // ============================
                        // LATEST DEPARTMENT
                        // ============================

                        departmentNo = reader["DepartmentNo"] == DBNull.Value
                            ? null
                            : (int?)Convert.ToInt32(reader["DepartmentNo"])
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================
        // GOAT INSPECTION / WEIGHT ENTRY (WEB PORTAL)
        // ============================
        [HttpPost("inspection/save")]
        public async Task<IActionResult> SaveInspection([FromBody] GoatInspectionRecord model)
        {
            if (model == null || model.AnimalNo <= 0)
                return BadRequest("Please select a valid Animal No.");

            if (model.RecordDate == default)
                return BadRequest("Please select the recording date.");

            if (model.Weight.HasValue && model.Weight.Value < 0)
                return BadRequest("Weight cannot be negative.");

            if (model.Teeth.HasValue &&
                (model.Teeth.Value < 0 || model.Teeth.Value > 8 || model.Teeth.Value % 2 != 0))
                return BadRequest("Teeth must be 0, 2, 4, 6 or 8.");

            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                const string findAnimalSql = @"
                    SELECT ""ATCode""
                    FROM ""ArticleInfo01""
                    WHERE ""AnimalNo"" = @AnimalNo
                      AND COALESCE(""GoatStatus"", 'Active') = 'Active'";

                string atCode;
                await using (var findCmd = new NpgsqlCommand(findAnimalSql, conn))
                {
                    findCmd.Parameters.AddWithValue("@AnimalNo", model.AnimalNo);
                    var result = await findCmd.ExecuteScalarAsync();
                    if (result == null || result == DBNull.Value)
                        return NotFound("Animal No not found.");
                    atCode = Convert.ToString(result)!;
                }

                const string insertSql = @"
                    INSERT INTO ""ArticleInfo03""
                    (""ATCode"", ""RecordDate"", ""Weight"", ""Teeth"", ""Nails"", ""Hair"")
                    VALUES
                    (@ATCode, @RecordDate, @Weight, @Teeth, @Nails, @Hair)";

                await using var cmd = new NpgsqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("@ATCode", atCode);
                cmd.Parameters.AddWithValue("@RecordDate", NpgsqlDbType.Timestamp, model.RecordDate);
                cmd.Parameters.AddWithValue("@Weight", NpgsqlDbType.Numeric,
                    model.Weight.HasValue ? (object)model.Weight.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Teeth", NpgsqlDbType.Integer,
                    model.Teeth.HasValue ? (object)model.Teeth.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Nails", NpgsqlDbType.Boolean,
                    model.Nails.HasValue ? (object)model.Nails.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Hair", NpgsqlDbType.Boolean,
                    model.Hair.HasValue ? (object)model.Hair.Value : DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Goat inspection record saved successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================
        // ITEM MASTER
        // ============================
        public class ItemSaveModel
        {
            public int? ItemID { get; set; }
            public string? ItemName { get; set; }
            public string? ItemType { get; set; }
            public string? BaseUnit { get; set; }
            public string? Description { get; set; }
            public bool IsActive { get; set; } = true;
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetItems()
        {
            try
            {
                var items = new List<object>();
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                const string sql = @"
                    SELECT ""ItemID"", ""ItemName"", ""ItemType"", ""BaseUnit"",
                           ""Description"", ""IsActive"", ""CreatedAt""
                    FROM public.""Item""
                    ORDER BY ""ItemName"", ""ItemID""";

                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    items.Add(new
                    {
                        itemID = Convert.ToInt32(reader["ItemID"]),
                        itemName = reader["ItemName"].ToString(),
                        itemType = reader["ItemType"].ToString(),
                        baseUnit = reader["BaseUnit"].ToString(),
                        description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
                        isActive = Convert.ToBoolean(reader["IsActive"]),
                        createdAt = reader["CreatedAt"] == DBNull.Value ? null : reader["CreatedAt"]
                    });
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("items/save")]
        public async Task<IActionResult> SaveItem([FromBody] ItemSaveModel model)
        {
            if (model == null)
                return BadRequest("Item details are required.");

            var itemName = (model.ItemName ?? string.Empty).Trim();
            var itemType = (model.ItemType ?? string.Empty).Trim().ToUpperInvariant();
            var baseUnit = (model.BaseUnit ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(itemName))
                return BadRequest("Item Name is required.");
            if (itemName.Length > 100)
                return BadRequest("Item Name cannot exceed 100 characters.");
            if (itemType != "FINISHED_FOOD" && itemType != "RAW_MATERIAL")
                return BadRequest("Item Type must be FINISHED_FOOD or RAW_MATERIAL.");
            if (string.IsNullOrWhiteSpace(baseUnit))
                return BadRequest("Base Unit is required.");
            if (baseUnit.Length > 20)
                return BadRequest("Base Unit cannot exceed 20 characters.");

            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                await using var transaction = await conn.BeginTransactionAsync();

                const string duplicateSql = @"
                    SELECT COUNT(*)
                    FROM public.""Item""
                    WHERE LOWER(""ItemName"") = LOWER(@ItemName)
                      AND (@ItemID IS NULL OR ""ItemID"" <> @ItemID)";

                await using (var duplicateCmd = new NpgsqlCommand(duplicateSql, conn, transaction))
                {
                    duplicateCmd.Parameters.AddWithValue("@ItemName", itemName);
                    duplicateCmd.Parameters.AddWithValue("@ItemID", NpgsqlDbType.Integer,
                        model.ItemID.HasValue ? (object)model.ItemID.Value : DBNull.Value);

                    if (Convert.ToInt32(await duplicateCmd.ExecuteScalarAsync()) > 0)
                        return Conflict("An item with this name already exists.");
                }

                if (model.ItemID.HasValue)
                {
                    const string updateSql = @"
                        UPDATE public.""Item""
                        SET ""ItemName"" = @ItemName,
                            ""ItemType"" = @ItemType,
                            ""BaseUnit"" = @BaseUnit,
                            ""Description"" = @Description,
                            ""IsActive"" = @IsActive
                        WHERE ""ItemID"" = @ItemID";

                    await using var cmd = new NpgsqlCommand(updateSql, conn, transaction);
                    cmd.Parameters.AddWithValue("@ItemName", itemName);
                    cmd.Parameters.AddWithValue("@ItemType", itemType);
                    cmd.Parameters.AddWithValue("@BaseUnit", baseUnit);
                    cmd.Parameters.AddWithValue("@Description", NpgsqlDbType.Text,
                        string.IsNullOrWhiteSpace(model.Description) ? (object)DBNull.Value : model.Description.Trim());
                    cmd.Parameters.AddWithValue("@IsActive", model.IsActive);
                    cmd.Parameters.AddWithValue("@ItemID", model.ItemID.Value);

                    if (await cmd.ExecuteNonQueryAsync() == 0)
                        return NotFound("Item not found.");
                }
                else
                {
                    const string insertSql = @"
                        INSERT INTO public.""Item""
                            (""ItemName"", ""ItemType"", ""BaseUnit"", ""Description"", ""IsActive"")
                        VALUES
                            (@ItemName, @ItemType, @BaseUnit, @Description, @IsActive)";

                    await using var cmd = new NpgsqlCommand(insertSql, conn, transaction);
                    cmd.Parameters.AddWithValue("@ItemName", itemName);
                    cmd.Parameters.AddWithValue("@ItemType", itemType);
                    cmd.Parameters.AddWithValue("@BaseUnit", baseUnit);
                    cmd.Parameters.AddWithValue("@Description", NpgsqlDbType.Text,
                        string.IsNullOrWhiteSpace(model.Description) ? (object)DBNull.Value : model.Description.Trim());
                    cmd.Parameters.AddWithValue("@IsActive", model.IsActive);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return Ok(new { message = model.ItemID.HasValue ? "Item updated successfully." : "Item added successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        // ============================
        // FOOD RECIPE
        // ============================
        public class FoodRecipeSaveModel
        {
            public long? RecipeID { get; set; }
            public int FinishedFoodID { get; set; }
            public string? RecipeName { get; set; }
            public int VersionNo { get; set; } = 1;
            public decimal BatchOutputQty { get; set; } = 1;
            public DateTime EffectiveDate { get; set; } = DateTime.Today;
            public bool IsActive { get; set; } = true;
            public List<FoodRecipeLineModel> Details { get; set; } = new();
        }
        public class FoodRecipeLineModel
        {
            public int RawMaterialID { get; set; }
            public decimal Quantity { get; set; }
        }

        [HttpGet("food/recipes")]
        public async Task<IActionResult> GetFoodRecipes()
        {
            try
            {
                var rows = new List<object>();
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                const string sql = @"
                    SELECT r.""RecipeID"", r.""FinishedFoodID"", f.""ItemName"" AS ""FinishedFoodName"",
                           r.""RecipeName"", r.""VersionNo"", r.""BatchOutputQty"", r.""EffectiveDate"", r.""IsActive"",
                           d.""RecipeDetailID"", d.""RawMaterialID"", i.""ItemName"" AS ""IngredientName"",
                           i.""ItemType"" AS ""IngredientType"", i.""BaseUnit"", d.""Quantity""
                    FROM public.""FoodRecipe"" r
                    JOIN public.""Item"" f ON f.""ItemID""=r.""FinishedFoodID""
                    LEFT JOIN public.""FoodRecipeDetail"" d ON d.""RecipeID""=r.""RecipeID""
                    LEFT JOIN public.""Item"" i ON i.""ItemID""=d.""RawMaterialID""
                    ORDER BY r.""FinishedFoodID"", r.""VersionNo"" DESC, d.""RecipeDetailID""";
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var rd = await cmd.ExecuteReaderAsync();
                var map = new Dictionary<long, dynamic>();
                while (await rd.ReadAsync())
                {
                    long id = Convert.ToInt64(rd["RecipeID"]);
                    if (!map.ContainsKey(id))
                        map[id] = new { recipeID = id, finishedFoodID = Convert.ToInt32(rd["FinishedFoodID"]), finishedFoodName = rd["FinishedFoodName"].ToString(), recipeName = rd["RecipeName"].ToString(), versionNo = Convert.ToInt32(rd["VersionNo"]), batchOutputQty = Convert.ToDecimal(rd["BatchOutputQty"]), effectiveDate = SafeDate(rd["EffectiveDate"])?.ToString("yyyy-MM-dd"), isActive = Convert.ToBoolean(rd["IsActive"]), details = new List<object>() };
                    if (rd["RecipeDetailID"] != DBNull.Value)
                    {
                        var details = (List<object>)map[id].details;
                        details.Add(new { rawMaterialID = Convert.ToInt32(rd["RawMaterialID"]), itemName = rd["IngredientName"].ToString(), itemType = rd["IngredientType"].ToString(), baseUnit = rd["BaseUnit"].ToString(), quantity = Convert.ToDecimal(rd["Quantity"]) });
                    }
                }
                return Ok(map.Values);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("food/recipes/save")]
        public async Task<IActionResult> SaveFoodRecipe([FromBody] FoodRecipeSaveModel model)
        {
            if (model == null || model.FinishedFoodID <= 0 || model.BatchOutputQty <= 0 || model.Details == null || model.Details.Count == 0)
                return BadRequest("Select a finished food, enter a positive batch output, and add at least one ingredient.");
            if (model.Details.Any(x => x.RawMaterialID <= 0 || x.Quantity <= 0)) return BadRequest("Every ingredient must have a valid item and quantity greater than zero.");
            if (model.Details.GroupBy(x => x.RawMaterialID).Any(g => g.Count() > 1)) return BadRequest("An item can only be added once in a recipe.");
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                await using var tx = await conn.BeginTransactionAsync();
                const string validSql = @"SELECT COUNT(*) FROM public.""Item"" WHERE (""ItemID""=@FinishedFoodID AND ""ItemType""='FINISHED_FOOD' AND ""IsActive""=TRUE) OR (""ItemID"" = ANY(@IngredientIDs) AND ""IsActive""=TRUE)";
                await using (var v = new NpgsqlCommand(validSql, conn, tx))
                {
                    v.Parameters.AddWithValue("@FinishedFoodID", model.FinishedFoodID);
                    v.Parameters.AddWithValue("@IngredientIDs", model.Details.Select(x => x.RawMaterialID).ToArray());
                    int count = Convert.ToInt32(await v.ExecuteScalarAsync());
                    if (count != model.Details.Count + 1) return BadRequest("Finished Food or one or more ingredients do not exist or are inactive.");
                }
                long recipeId;
                if (model.RecipeID.HasValue)
                {
                    const string upd = @"UPDATE public.""FoodRecipe"" SET ""FinishedFoodID""=@FinishedFoodID,""RecipeName""=@RecipeName,""VersionNo""=@VersionNo,""BatchOutputQty""=@BatchOutputQty,""EffectiveDate""=@EffectiveDate,""IsActive""=@IsActive WHERE ""RecipeID""=@RecipeID";
                    await using var u = new NpgsqlCommand(upd, conn, tx);
                    AddRecipeParams(u, model); u.Parameters.AddWithValue("@RecipeID", model.RecipeID.Value);
                    if (await u.ExecuteNonQueryAsync() == 0) return NotFound("Recipe not found.");
                    recipeId = model.RecipeID.Value;
                    await using var del = new NpgsqlCommand(@"DELETE FROM public.""FoodRecipeDetail"" WHERE ""RecipeID""=@RecipeID", conn, tx);
                    del.Parameters.AddWithValue("@RecipeID", recipeId); await del.ExecuteNonQueryAsync();
                }
                else
                {
                    const string ins = @"INSERT INTO public.""FoodRecipe"" (""FinishedFoodID"",""RecipeName"",""VersionNo"",""BatchOutputQty"",""EffectiveDate"",""IsActive"") VALUES (@FinishedFoodID,@RecipeName,@VersionNo,@BatchOutputQty,@EffectiveDate,@IsActive) RETURNING ""RecipeID""";
                    await using var i = new NpgsqlCommand(ins, conn, tx); AddRecipeParams(i, model); recipeId = Convert.ToInt64(await i.ExecuteScalarAsync());
                }
                foreach (var line in model.Details)
                {
                    const string detailSql = @"INSERT INTO public.""FoodRecipeDetail"" (""RecipeID"",""RawMaterialID"",""Quantity"") VALUES (@RecipeID,@RawMaterialID,@Quantity)";
                    await using var d = new NpgsqlCommand(detailSql, conn, tx);
                    d.Parameters.AddWithValue("@RecipeID", recipeId); d.Parameters.AddWithValue("@RawMaterialID", line.RawMaterialID); d.Parameters.AddWithValue("@Quantity", line.Quantity); await d.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync(); return Ok(new { message = "Recipe saved successfully.", recipeID = recipeId });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
        private static void AddRecipeParams(NpgsqlCommand c, FoodRecipeSaveModel m)
        {
            c.Parameters.AddWithValue("@FinishedFoodID", m.FinishedFoodID);
            c.Parameters.AddWithValue("@RecipeName", (object?)m.RecipeName?.Trim() ?? DBNull.Value);
            c.Parameters.AddWithValue("@VersionNo", m.VersionNo <= 0 ? 1 : m.VersionNo);
            c.Parameters.AddWithValue("@BatchOutputQty", m.BatchOutputQty);
            c.Parameters.AddWithValue("@EffectiveDate", NpgsqlDbType.Date, m.EffectiveDate.Date);
            c.Parameters.AddWithValue("@IsActive", m.IsActive);
        }

        // ============================
        // DAILY FOOD ISSUE
        // ============================
        public class FoodIssueSaveModel { public DateTime IssueDate { get; set; } public int FoodItemID { get; set; } public List<FoodIssueLineModel> Details { get; set; } = new(); }
        public class FoodIssueLineModel { public int DepartmentID { get; set; } public int GoatCount { get; set; } public decimal TotalActiveWeight { get; set; } public decimal CalculatedFood { get; set; } public decimal FoodIssued { get; set; } }

        [HttpGet("food/issues/dates")]
        public async Task<IActionResult> GetFoodIssueDates()
        {
            try { var dates = new List<string>(); await using var c = new NpgsqlConnection(connectionString); await c.OpenAsync(); await using var q = new NpgsqlCommand(@"SELECT DISTINCT ""IssueDate"" FROM public.""FoodIssue"" ORDER BY ""IssueDate""", c); await using var r = await q.ExecuteReaderAsync(); while (await r.ReadAsync()) dates.Add(r.GetFieldValue<DateOnly>(0).ToString("yyyy-MM-dd")); return Ok(dates); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("food/issues/current")]
        public async Task<IActionResult> GetFoodIssueCurrent(
            [FromQuery] DateTime? date,
            [FromQuery] int? foodItemID)
        {
            try
            {
                await using var c = new NpgsqlConnection(connectionString);
                await c.OpenAsync();

                if (date.HasValue)
                {
                    const string savedSql = @"
                        WITH selected_issue AS
                        (
                            SELECT ""FoodIssueID"", ""IssueDate"", ""FoodID""
                            FROM public.""FoodIssue""
                            WHERE ""IssueDate"" = @Date
                              AND (@FoodItemID IS NULL OR ""FoodID"" = @FoodItemID)
                            ORDER BY ""FoodIssueID"" DESC
                            LIMIT 1
                        )
                        SELECT
                            h.""FoodIssueID"",
                            h.""IssueDate"",
                            h.""FoodID"" AS ""FoodItemID"",
                            d.""DepartmentID"",
                            dp.""DepartmentName"",
                            d.""GoatCount"",
                            d.""TotalActiveWeight"",
                            d.""CalculatedFood"",
                            d.""FoodIssued""
                        FROM selected_issue h
                        JOIN public.""FoodIssueDetail"" d
                            ON d.""FoodIssueID"" = h.""FoodIssueID""
                        JOIN public.""Department"" dp
                            ON dp.""DepartmentID"" = d.""DepartmentID""
                        ORDER BY dp.""DepartmentName""";

                    var saved = new List<object>();
                    long? id = null;
                    int? savedFoodItemID = null;

                    await using var q = new NpgsqlCommand(savedSql, c);
                    q.Parameters.AddWithValue("@Date", NpgsqlDbType.Date, date.Value.Date);
                    q.Parameters.AddWithValue("@FoodItemID", NpgsqlDbType.Bigint,
                        foodItemID.HasValue ? (object)(long)foodItemID.Value : DBNull.Value);

                    await using var r = await q.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        id = Convert.ToInt64(r["FoodIssueID"]);
                        savedFoodItemID = Convert.ToInt32(r["FoodItemID"]);
                        saved.Add(new
                        {
                            departmentID = Convert.ToInt32(r["DepartmentID"]),
                            departmentName = r["DepartmentName"].ToString(),
                            goatCount = Convert.ToInt32(r["GoatCount"]),
                            totalActiveWeight = Convert.ToDecimal(r["TotalActiveWeight"]),
                            calculatedFood = Convert.ToDecimal(r["CalculatedFood"]),
                            foodIssued = Convert.ToDecimal(r["FoodIssued"])
                        });
                    }

                    if (id.HasValue)
                    {
                        return Ok(new
                        {
                            foodIssueID = id,
                            foodItemID = savedFoodItemID,
                            issueDate = date.Value.ToString("yyyy-MM-dd"),
                            isSaved = true,
                            details = saved
                        });
                    }
                }

                const string currentSql = @"
                    WITH active_goats AS
                    (
                        SELECT a.""ATCode"", COALESCE(dep.""DepartmentID"", -1) AS ""DepartmentID""
                        FROM public.""ArticleInfo01"" a
                        LEFT JOIN LATERAL
                        (
                            SELECT ""DepartmentID""
                            FROM public.""AnimalDepartment""
                            WHERE ""ATCode"" = a.""ATCode""
                            ORDER BY ""EffectiveDate"" DESC, ""AssignmentID"" DESC
                            LIMIT 1
                        ) dep ON TRUE
                        WHERE COALESCE(a.""GoatStatus"", 'Active') = 'Active'
                          AND dep.""DepartmentID"" IS NOT NULL
                    ),
                    latest_weight AS
                    (
                        SELECT DISTINCT ON (""ATCode"") ""ATCode"", ""Weight""
                        FROM public.""ArticleInfo03""
                        WHERE ""Weight"" IS NOT NULL
                        ORDER BY ""ATCode"", ""RecordDate"" DESC, recordid DESC
                    )
                    SELECT
                        d.""DepartmentID"",
                        d.""DepartmentName"",
                        COUNT(g.""ATCode"")::int AS ""GoatCount"",
                        COALESCE(SUM(w.""Weight""), 0)::numeric(12,2) AS ""TotalActiveWeight"",
                        (COALESCE(SUM(w.""Weight""), 0) * 0.02)::numeric(12,2) AS ""CalculatedFood""
                    FROM public.""Department"" d
                    LEFT JOIN active_goats g ON g.""DepartmentID"" = d.""DepartmentID""
                    LEFT JOIN latest_weight w ON w.""ATCode"" = g.""ATCode""
                    WHERE d.""IsActive"" = TRUE
                    GROUP BY d.""DepartmentID"", d.""DepartmentName""
                    ORDER BY d.""DepartmentName""";

                var list = new List<object>();
                await using (var q = new NpgsqlCommand(currentSql, c))
                await using (var r = await q.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        decimal calc = Convert.ToDecimal(r["CalculatedFood"]);
                        list.Add(new
                        {
                            departmentID = Convert.ToInt32(r["DepartmentID"]),
                            departmentName = r["DepartmentName"].ToString(),
                            goatCount = Convert.ToInt32(r["GoatCount"]),
                            totalActiveWeight = Convert.ToDecimal(r["TotalActiveWeight"]),
                            calculatedFood = calc,
                            foodIssued = calc
                        });
                    }
                }

                return Ok(new
                {
                    foodIssueID = (long?)null,
                    foodItemID = foodItemID,
                    issueDate = (date ?? DateTime.Today).ToString("yyyy-MM-dd"),
                    isSaved = false,
                    details = list
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("food/issues/save")]
        public async Task<IActionResult> SaveFoodIssue([FromBody] FoodIssueSaveModel model)
        {
            if (model == null || model.IssueDate == default || model.FoodItemID <= 0 ||
                model.Details == null || model.Details.Count == 0)
                return BadRequest("Issue date, Finished Food item and department details are required.");

            if (model.Details.Any(x => x.DepartmentID <= 0 || x.GoatCount < 0 ||
                x.TotalActiveWeight < 0 || x.CalculatedFood < 0 || x.FoodIssued < 0))
                return BadRequest("Invalid food issue values.");

            if (!model.Details.Any(x => x.FoodIssued > 0))
                return BadRequest("Enter Food Issued greater than zero for at least one department.");

            try
            {
                await using var c = new NpgsqlConnection(connectionString);
                await c.OpenAsync();
                await using var tx = await c.BeginTransactionAsync();

                const string itemCheckSql = @"
                    SELECT COUNT(*)
                    FROM public.""Item""
                    WHERE ""ItemID"" = @ItemID
                      AND ""ItemType"" = 'FINISHED_FOOD'
                      AND ""IsActive"" = TRUE";

                await using (var itemCheck = new NpgsqlCommand(itemCheckSql, c, tx))
                {
                    itemCheck.Parameters.AddWithValue("@ItemID", model.FoodItemID);
                    if (Convert.ToInt32(await itemCheck.ExecuteScalarAsync()) != 1)
                        return BadRequest("Select an active Finished Food item.");
                }

                const string headSql = @"
                    INSERT INTO public.""FoodIssue"" (""IssueDate"", ""FoodID"")
                    VALUES (@IssueDate, @FoodID)
                    ON CONFLICT (""IssueDate"", ""FoodID"") DO NOTHING
                    RETURNING ""FoodIssueID""";

                long issueId;
                await using (var h = new NpgsqlCommand(headSql, c, tx))
                {
                    h.Parameters.AddWithValue("@IssueDate", NpgsqlDbType.Date, model.IssueDate.Date);
                    h.Parameters.AddWithValue("@FoodID", NpgsqlDbType.Bigint, (long)model.FoodItemID);

                    var value = await h.ExecuteScalarAsync();
                    if (value == null)
                    {
                        await tx.RollbackAsync();
                        return Conflict("A food issue already exists for this date and food item. Use Edit instead.");
                    }

                    issueId = Convert.ToInt64(value);
                }

                const string detailSql = @"
                    INSERT INTO public.""FoodIssueDetail""
                    (""FoodIssueID"", ""DepartmentID"", ""GoatCount"", ""TotalActiveWeight"",
                     ""CalculatedFood"", ""FoodIssued"", ""FoodItemID"")
                    VALUES
                    (@ID, @DepartmentID, @GoatCount, @Weight, @Calculated, @Issued, @FoodItemID)";

                foreach (var x in model.Details.Where(x => x.FoodIssued > 0))
                {
                    await using var q = new NpgsqlCommand(detailSql, c, tx);
                    q.Parameters.AddWithValue("@ID", issueId);
                    q.Parameters.AddWithValue("@DepartmentID", x.DepartmentID);
                    q.Parameters.AddWithValue("@GoatCount", x.GoatCount);
                    q.Parameters.AddWithValue("@Weight", x.TotalActiveWeight);
                    q.Parameters.AddWithValue("@Calculated", x.CalculatedFood);
                    q.Parameters.AddWithValue("@Issued", x.FoodIssued);
                    q.Parameters.AddWithValue("@FoodItemID", model.FoodItemID);
                    await q.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return Ok(new { message = "Daily food issue saved.", foodIssueID = issueId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("food/issues/update")]
        public async Task<IActionResult> UpdateFoodIssue([FromBody] FoodIssueSaveModel model)
        {
            if (model == null || model.IssueDate == default || model.FoodItemID <= 0 ||
                model.Details == null || model.Details.Count == 0)
                return BadRequest("Issue date, Finished Food item and details are required.");

            if (model.Details.Any(x => x.DepartmentID <= 0 || x.GoatCount < 0 ||
                x.TotalActiveWeight < 0 || x.CalculatedFood < 0 || x.FoodIssued < 0))
                return BadRequest("Invalid food issue values.");

            if (!model.Details.Any(x => x.FoodIssued > 0))
                return BadRequest("Enter Food Issued greater than zero for at least one department.");

            try
            {
                await using var c = new NpgsqlConnection(connectionString);
                await c.OpenAsync();
                await using var tx = await c.BeginTransactionAsync();

                const string itemCheckSql = @"
                    SELECT COUNT(*)
                    FROM public.""Item""
                    WHERE ""ItemID"" = @ItemID
                      AND ""ItemType"" = 'FINISHED_FOOD'
                      AND ""IsActive"" = TRUE";

                await using (var itemCheck = new NpgsqlCommand(itemCheckSql, c, tx))
                {
                    itemCheck.Parameters.AddWithValue("@ItemID", model.FoodItemID);
                    if (Convert.ToInt32(await itemCheck.ExecuteScalarAsync()) != 1)
                        return BadRequest("Select an active Finished Food item.");
                }

                const string findSql = @"
                    SELECT ""FoodIssueID""
                    FROM public.""FoodIssue""
                    WHERE ""IssueDate"" = @Date AND ""FoodID"" = @FoodID
                    FOR UPDATE";

                long issueId;
                await using (var h = new NpgsqlCommand(findSql, c, tx))
                {
                    h.Parameters.AddWithValue("@Date", NpgsqlDbType.Date, model.IssueDate.Date);
                    h.Parameters.AddWithValue("@FoodID", NpgsqlDbType.Bigint, (long)model.FoodItemID);
                    var value = await h.ExecuteScalarAsync();
                    if (value == null)
                        return NotFound("Food issue not found for the selected date and food item.");
                    issueId = Convert.ToInt64(value);
                }

                await using (var d = new NpgsqlCommand(
                    @"DELETE FROM public.""FoodIssueDetail"" WHERE ""FoodIssueID"" = @ID", c, tx))
                {
                    d.Parameters.AddWithValue("@ID", issueId);
                    await d.ExecuteNonQueryAsync();
                }

                const string detailSql = @"
                    INSERT INTO public.""FoodIssueDetail""
                    (""FoodIssueID"", ""DepartmentID"", ""GoatCount"", ""TotalActiveWeight"",
                     ""CalculatedFood"", ""FoodIssued"", ""FoodItemID"")
                    VALUES
                    (@ID, @DepartmentID, @GoatCount, @Weight, @Calculated, @Issued, @FoodItemID)";

                foreach (var x in model.Details.Where(x => x.FoodIssued > 0))
                {
                    await using var q = new NpgsqlCommand(detailSql, c, tx);
                    q.Parameters.AddWithValue("@ID", issueId);
                    q.Parameters.AddWithValue("@DepartmentID", x.DepartmentID);
                    q.Parameters.AddWithValue("@GoatCount", x.GoatCount);
                    q.Parameters.AddWithValue("@Weight", x.TotalActiveWeight);
                    q.Parameters.AddWithValue("@Calculated", x.CalculatedFood);
                    q.Parameters.AddWithValue("@Issued", x.FoodIssued);
                    q.Parameters.AddWithValue("@FoodItemID", model.FoodItemID);
                    await q.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return Ok(new { message = "Daily food issue updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("food/issues/delete")]
        public async Task<IActionResult> DeleteFoodIssue(
            [FromQuery] DateTime date,
            [FromQuery] int foodItemID)
        {
            if (date == default || foodItemID <= 0)
                return BadRequest("Issue date and food item are required.");

            try
            {
                await using var c = new NpgsqlConnection(connectionString);
                await c.OpenAsync();

                const string deleteSql = @"
                    DELETE FROM public.""FoodIssue""
                    WHERE ""IssueDate"" = @Date AND ""FoodID"" = @FoodID";

                await using var q = new NpgsqlCommand(deleteSql, c);
                q.Parameters.AddWithValue("@Date", NpgsqlDbType.Date, date.Date);
                q.Parameters.AddWithValue("@FoodID", NpgsqlDbType.Bigint, (long)foodItemID);

                int rows = await q.ExecuteNonQueryAsync();
                return rows == 0
                    ? NotFound("Food issue not found for the selected date and food item.")
                    : Ok(new { message = "Daily food issue deleted." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}