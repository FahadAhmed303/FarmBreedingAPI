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
    public class CategoryController : ControllerBase
    {
        private readonly string connectionString =
"Host=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.lnndywzphqtvzcvunmqc;Password=qAZVexd1DM2Ya2UE;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;Timeout=15;Command Timeout=30";

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var list = new List<Category>();

            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"SELECT ""atcategorycode"", ""atcategoryname"" 
                               FROM ""ATCategory""";

                await using var cmd = new NpgsqlCommand(sql, conn);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new Category
                        {
                            ATCategoryCode = reader["atcategorycode"]?.ToString(),
                            ATCategoryName = reader["atcategoryname"]?.ToString()
                        });
                    }
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}