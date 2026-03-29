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
    public class GrowthRecordController : ControllerBase
    {
        private readonly string connectionString =
"Host=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.lnndywzphqtvzcvunmqc;Password=qAZVexd1DM2Ya2UE;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;Timeout=15;Command Timeout=30";

        // SAVE GROWTH RECORD
        [HttpPost]
        public async Task<IActionResult> SaveGrowthRecord([FromBody] GrowthRecord record)
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

                cmd.Parameters.AddWithValue("@ATCode", record.ATCode);
                cmd.Parameters.AddWithValue("@RecordDate", record.RecordDate);
                cmd.Parameters.AddWithValue("@Weight", record.Weight);
                cmd.Parameters.AddWithValue("@Height", record.Height);
                cmd.Parameters.AddWithValue("@Width", record.Width);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new { message = "Growth record saved." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET GROWTH HISTORY
        [HttpGet("{atcode}")]
        public async Task<IActionResult> GetGrowthRecords(string atcode)
        {
            var list = new List<GrowthRecord>();

            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT ""ATCode"", ""RecordDate"", ""Weight"", ""Height"", ""Width""
                    FROM ""ArticleInfo03""
                    WHERE ""ATCode""=@ATCode
                    ORDER BY ""RecordDate"" DESC";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ATCode", atcode);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new GrowthRecord
                    {
                        ATCode = reader["ATCode"]?.ToString(), // ✅ FIXED
                        RecordDate = reader["RecordDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["RecordDate"]),
                        Weight = reader["Weight"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Weight"]),
                        Height = reader["Height"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Height"]),
                        Width = reader["Width"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Width"])
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}