using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FarmBreedingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhotoController : ControllerBase
    {
        private readonly string connectionString =
"Host=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.lnndywzphqtvzcvunmqc;Password=qAZVexd1DM2Ya2UE;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;Timeout=15;Command Timeout=30";

        //-----------------------------------------
        // 1️⃣ SAVE PHOTO URLs (NO AZURE)
        //-----------------------------------------
        [HttpPost("upload")]
        public async Task<IActionResult> Upload()
        {
            try
            {
                string atcode = Request.Form["atcode"].ToString();

                string url1 = Request.Form["photo1"];
                string url2 = Request.Form["photo2"];
                string url3 = Request.Form["photo3"];

                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                INSERT INTO ""ArticleInfo02""
                (""ATCode"", ""photodate"", photo01, photo02, photo03)
                VALUES
                (@ATCode, NOW(), @Photo01, @Photo02, @Photo03)";

                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@ATCode", atcode);
                cmd.Parameters.AddWithValue("@Photo01", string.IsNullOrEmpty(url1) ? (object)DBNull.Value : url1);
                cmd.Parameters.AddWithValue("@Photo02", string.IsNullOrEmpty(url2) ? (object)DBNull.Value : url2);
                cmd.Parameters.AddWithValue("@Photo03", string.IsNullOrEmpty(url3) ? (object)DBNull.Value : url3);

                await cmd.ExecuteNonQueryAsync();

                return Ok("Photos saved");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //-----------------------------------------
        // 2️⃣ GET PHOTO HISTORY
        //-----------------------------------------
        [HttpGet("{atcode}")]
        public async Task<IActionResult> GetPhotos(string atcode)
        {
            var list = new List<object>();

            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                SELECT ""photodate"", photo01, photo02, photo03
                FROM ""ArticleInfo02""
                WHERE ""ATCode""=@ATCode
                ORDER BY ""photodate"" DESC";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ATCode", atcode);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        PhotoDate = reader["photodate"],
                        Photo01 = reader["photo01"] == DBNull.Value ? null : reader["photo01"].ToString(),
                        Photo02 = reader["photo02"] == DBNull.Value ? null : reader["photo02"].ToString(),
                        Photo03 = reader["photo03"] == DBNull.Value ? null : reader["photo03"].ToString()
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //-----------------------------------------
        // 3️⃣ DELETE PHOTO (DB ONLY)
        //-----------------------------------------
        [HttpDelete]
        public async Task<IActionResult> Delete(string url)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                UPDATE ""ArticleInfo02""
                SET photo01=NULL, photo02=NULL, photo03=NULL
                WHERE photo01=@url OR photo02=@url OR photo03=@url";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@url", url);

                await cmd.ExecuteNonQueryAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}