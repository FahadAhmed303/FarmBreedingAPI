using Microsoft.AspNetCore.Mvc;
using Npgsql;
using FarmBreedingAPI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FarmBreedingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnimalController : ControllerBase
    {
        private readonly string connectionString =
"Host=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.lnndywzphqtvzcvunmqc;Password=qAZVexd1DM2Ya2UE;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;Timeout=15;Command Timeout=30";

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
                       sourcetype, purchasedate, price, agentname, mothercode
                FROM ""ArticleInfo01"" 
                WHERE ""ATCode""=@ATCode";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ATCode", atcode);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        DateTime? dobValue = null;

                        if (reader["DOB"] != DBNull.Value)
                        {
                            if (reader["DOB"] is DateTime dt)
                                dobValue = dt;
                            else if (reader["DOB"] is DateOnly d)
                                dobValue = d.ToDateTime(TimeOnly.MinValue);
                        }

                        return Ok(new
                        {
                            atCode = reader["ATCode"].ToString(),
                            gender = reader["gender"]?.ToString(),
                            atCategoryCode = reader["ATCategoryCode"]?.ToString(),
                            dob = dobValue,

                            sourceType = reader["sourcetype"]?.ToString(),
                            purchaseDate = reader["purchasedate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["purchasedate"]),
                            price = reader["price"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["price"]),
                            agentName = reader["agentname"]?.ToString(),
                            motherCode = reader["mothercode"]?.ToString()
                        });
                    }
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================
        // 2. FETCH GROWTH
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
                            weight = reader["Weight"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Weight"]),
                            height = reader["Height"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Height"]),
                            width = reader["Width"] == DBNull.Value ? 0 : Convert.ToDouble(reader["Width"]),
                            recordDate = reader["RecordDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["RecordDate"])
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
        // 4. SAVE ANIMAL (UPDATED)
        // ============================
        [HttpPost("save")]
        public async Task<IActionResult> SaveAnimal([FromBody] Animal model)
        {
            try
            {
                // 🔥 BUSINESS LOGIC
                if (model.SourceType == "PURCHASE")
                {
                    model.MotherCode = null;
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
                    mothercode = @MotherCode
                WHERE ""ATCode"" = @ATCode";

                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@ATCode", model.ATCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@gender", model.gender ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ATCategoryCode", model.ATCategoryCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@DOB", model.DOB ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@SourceType", model.SourceType ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PurchaseDate", model.PurchaseDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Price", model.Price ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@AgentName", model.AgentName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@MotherCode", model.MotherCode ?? (object)DBNull.Value);

                int rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    return NotFound("Animal not found");

                return Ok(new { message = "Updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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

                return Ok(new { message = "Growth saved successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}