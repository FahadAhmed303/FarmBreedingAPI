using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;

namespace FarmBreedingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhotoController : ControllerBase
    {
        private readonly string connectionString =
"Host=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.lnndywzphqtvzcvunmqc;Password=qAZVexd1DM2Ya2UE;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;Timeout=15;Command Timeout=30";

        private readonly Cloudinary _cloudinary;

        public PhotoController(IConfiguration config)
        {
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
        }

        //-----------------------------------------
        // 1️⃣ UPLOAD + SAVE (REAL IMAGES)
        //-----------------------------------------
        [HttpPost("upload")]
        public async Task<IActionResult> Upload()
        {
            try
            {
                string atcode = Request.Form["atcode"];

                string url1 = null;
                string url2 = null;
                string url3 = null;

                // PHOTO 1
                if (Request.Form.Files["photo1"] != null)
                {
                    var uploadResult = await UploadToCloudinary(Request.Form.Files["photo1"]);
                    url1 = uploadResult;
                }

                // PHOTO 2
                if (Request.Form.Files["photo2"] != null)
                {
                    var uploadResult = await UploadToCloudinary(Request.Form.Files["photo2"]);
                    url2 = uploadResult;
                }

                // PHOTO 3
                if (Request.Form.Files["photo3"] != null)
                {
                    var uploadResult = await UploadToCloudinary(Request.Form.Files["photo3"]);
                    url3 = uploadResult;
                }

                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                INSERT INTO ""ArticleInfo02""
                (""ATCode"", ""photodate"", photo01, photo02, photo03)
                VALUES
                (@ATCode, NOW(), @Photo01, @Photo02, @Photo03)";

                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@ATCode", atcode);
                cmd.Parameters.AddWithValue("@Photo01", (object?)url1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Photo02", (object?)url2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Photo03", (object?)url3 ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();

                return Ok("Photos uploaded + saved");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //-----------------------------------------
        // CLOUDINARY HELPER
        //-----------------------------------------
        private async Task<string> UploadToCloudinary(IFormFile file)
        {
            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, stream)
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            return result.SecureUrl.ToString();
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