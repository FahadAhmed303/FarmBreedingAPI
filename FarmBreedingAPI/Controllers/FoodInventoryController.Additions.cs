using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FarmBreedingAPI.Controllers
{
    public partial class AnimalController
    {
        // FOOD INVENTORY API ADDITIONS
        // Merge these members INSIDE the existing AnimalController class.
        // Change the existing declaration to: public partial class AnimalController : ControllerBase
        // Keep the existing private connectionString field in that partial class.
        // Requires Npgsql and NpgsqlTypes already imported by AnimalController.cs.

        public class FoodPurchaseLineInput
        {
            public int ItemID { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitRate { get; set; }
        }
        public class FoodPurchaseInput
        {
            public DateTime PurchaseDate { get; set; }
            public string? SupplierName { get; set; }
            public string? InvoiceNo { get; set; }
            public string? Remarks { get; set; }
            public List<FoodPurchaseLineInput> Details { get; set; } = new();
        }
        public class FoodWasteLineInput
        {
            public int ItemID { get; set; }
            public decimal Quantity { get; set; }
        }
        public class FoodWasteInput
        {
            public DateTime WasteDate { get; set; }
            public string Reason { get; set; } = "";
            public string? Remarks { get; set; }
            public List<FoodWasteLineInput> Details { get; set; } = new();
        }
        public class FoodProductionInput
        {
            public DateTime ProductionDate { get; set; }
            public long RecipeID { get; set; }
            public decimal ProducedQty { get; set; }
            public string? Remarks { get; set; }
        }

        [HttpPost("food/purchases/save")]
        public async Task<IActionResult> SaveFoodPurchase([FromBody] FoodPurchaseInput m)
        {
            if (m == null || m.PurchaseDate == default || m.Details == null || m.Details.Count == 0)
                return BadRequest("Purchase date and at least one item are required.");
            if (m.Details.Any(x => x.ItemID <= 0 || x.Quantity <= 0 || x.UnitRate < 0))
                return BadRequest("Each purchase line needs a valid item, positive quantity and non-negative rate.");
            if (m.Details.GroupBy(x => x.ItemID).Any(g => g.Count() > 1))
                return BadRequest("An item can appear only once in a purchase. Combine its quantity.");

            await using var c = new NpgsqlConnection(connectionString);
            await c.OpenAsync();
            await using var tx = await c.BeginTransactionAsync();
            try
            {
                foreach (var line in m.Details)
                {
                    await using var check = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM public.\"Item\" WHERE \"ItemID\"=@id AND \"IsActive\"=TRUE", c, tx);
                    check.Parameters.AddWithValue("@id", line.ItemID);
                    if (Convert.ToInt32(await check.ExecuteScalarAsync()) != 1)
                        throw new InvalidOperationException($"Item {line.ItemID} does not exist or is inactive.");
                }

                long purchaseId;
                const string headSql = @"INSERT INTO public.""FoodPurchase""
            (""PurchaseDate"",""SupplierName"",""InvoiceNo"",""Remarks"")
            VALUES (@date,@supplier,@invoice,@remarks) RETURNING ""PurchaseID""";
                await using (var h = new NpgsqlCommand(headSql, c, tx))
                {
                    h.Parameters.AddWithValue("@date", NpgsqlDbType.Date, m.PurchaseDate.Date);
                    h.Parameters.AddWithValue("@supplier", (object?)m.SupplierName?.Trim() ?? DBNull.Value);
                    h.Parameters.AddWithValue("@invoice", (object?)m.InvoiceNo?.Trim() ?? DBNull.Value);
                    h.Parameters.AddWithValue("@remarks", (object?)m.Remarks?.Trim() ?? DBNull.Value);
                    purchaseId = Convert.ToInt64(await h.ExecuteScalarAsync());
                }

                foreach (var line in m.Details)
                {
                    long detailId;
                    const string detailSql = @"INSERT INTO public.""FoodPurchaseDetail""
                (""PurchaseID"",""ItemID"",""Quantity"",""UnitRate"")
                VALUES (@purchase,@item,@qty,@rate) RETURNING ""PurchaseDetailID""";
                    await using (var d = new NpgsqlCommand(detailSql, c, tx))
                    {
                        d.Parameters.AddWithValue("@purchase", purchaseId);
                        d.Parameters.AddWithValue("@item", line.ItemID);
                        d.Parameters.AddWithValue("@qty", line.Quantity);
                        d.Parameters.AddWithValue("@rate", line.UnitRate);
                        detailId = Convert.ToInt64(await d.ExecuteScalarAsync());
                    }
                    const string ledgerSql = @"INSERT INTO public.""StockTransaction""
                (""TransactionDate"",""ItemID"",""TransactionType"",""SourceType"",""SourceID"",""SourceDetailID"",""QtyIn"")
                VALUES (@date,@item,'PURCHASE','FOOD_PURCHASE',@source,@detail,@qty)";
                    await using var l = new NpgsqlCommand(ledgerSql, c, tx);
                    l.Parameters.AddWithValue("@date", NpgsqlDbType.Date, m.PurchaseDate.Date);
                    l.Parameters.AddWithValue("@item", line.ItemID);
                    l.Parameters.AddWithValue("@source", purchaseId);
                    l.Parameters.AddWithValue("@detail", detailId);
                    l.Parameters.AddWithValue("@qty", line.Quantity);
                    await l.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
                return Ok(new { message = "Purchase saved and stock updated.", purchaseID = purchaseId });
            }
            catch (Exception ex) { await tx.RollbackAsync(); return BadRequest(ex.Message); }
        }

        [HttpPost("food/waste/save")]
        public async Task<IActionResult> SaveFoodWaste([FromBody] FoodWasteInput m)
        {
            if (m == null || m.WasteDate == default || string.IsNullOrWhiteSpace(m.Reason) || m.Details == null || m.Details.Count == 0)
                return BadRequest("Waste date, reason and at least one item are required.");
            if (m.Details.Any(x => x.ItemID <= 0 || x.Quantity <= 0))
                return BadRequest("Every waste line needs a valid item and positive quantity.");
            if (m.Details.GroupBy(x => x.ItemID).Any(g => g.Count() > 1))
                return BadRequest("An item can appear only once in a waste entry.");

            await using var c = new NpgsqlConnection(connectionString);
            await c.OpenAsync();
            await using var tx = await c.BeginTransactionAsync();
            try
            {
                long wasteId;
                const string headSql = @"INSERT INTO public.""FoodWaste"" (""WasteDate"",""Reason"",""Remarks"")
            VALUES (@date,@reason,@remarks) RETURNING ""WasteID""";
                await using (var h = new NpgsqlCommand(headSql, c, tx))
                {
                    h.Parameters.AddWithValue("@date", NpgsqlDbType.Date, m.WasteDate.Date);
                    h.Parameters.AddWithValue("@reason", m.Reason.Trim());
                    h.Parameters.AddWithValue("@remarks", (object?)m.Remarks?.Trim() ?? DBNull.Value);
                    wasteId = Convert.ToInt64(await h.ExecuteScalarAsync());
                }

                foreach (var line in m.Details)
                {
                    // Lock the item row to serialize stock validation against concurrent transactions.
                    await using var itemLock = new NpgsqlCommand(
                        "SELECT \"ItemID\" FROM public.\"Item\" WHERE \"ItemID\"=@id AND \"IsActive\"=TRUE FOR UPDATE", c, tx);
                    itemLock.Parameters.AddWithValue("@id", line.ItemID);
                    if (await itemLock.ExecuteScalarAsync() == null) throw new InvalidOperationException($"Item {line.ItemID} not found or inactive.");
                    await EnsureFoodStockAvailable(c, tx, line.ItemID, line.Quantity);

                    long detailId;
                    await using (var d = new NpgsqlCommand(@"INSERT INTO public.""FoodWasteDetail"" (""WasteID"",""ItemID"",""Quantity"")
                VALUES (@waste,@item,@qty) RETURNING ""WasteDetailID""", c, tx))
                    {
                        d.Parameters.AddWithValue("@waste", wasteId); d.Parameters.AddWithValue("@item", line.ItemID); d.Parameters.AddWithValue("@qty", line.Quantity);
                        detailId = Convert.ToInt64(await d.ExecuteScalarAsync());
                    }
                    await using var l = new NpgsqlCommand(@"INSERT INTO public.""StockTransaction""
                (""TransactionDate"",""ItemID"",""TransactionType"",""SourceType"",""SourceID"",""SourceDetailID"",""QtyOut"")
                VALUES (@date,@item,'WASTE','FOOD_WASTE',@source,@detail,@qty)", c, tx);
                    l.Parameters.AddWithValue("@date", NpgsqlDbType.Date, m.WasteDate.Date); l.Parameters.AddWithValue("@item", line.ItemID);
                    l.Parameters.AddWithValue("@source", wasteId); l.Parameters.AddWithValue("@detail", detailId); l.Parameters.AddWithValue("@qty", line.Quantity);
                    await l.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
                return Ok(new { message = "Waste saved and stock reduced.", wasteID = wasteId });
            }
            catch (Exception ex) { await tx.RollbackAsync(); return BadRequest(ex.Message); }
        }

        [HttpPost("food/production/save")]
        public async Task<IActionResult> SaveFoodProduction([FromBody] FoodProductionInput m)
        {
            if (m == null || m.ProductionDate == default || m.RecipeID <= 0 || m.ProducedQty <= 0)
                return BadRequest("Production date, recipe and positive produced quantity are required.");

            await using var c = new NpgsqlConnection(connectionString);
            await c.OpenAsync();
            await using var tx = await c.BeginTransactionAsync();
            try
            {
                int finishedFoodId; decimal batchOutput;
                await using (var recipe = new NpgsqlCommand(@"SELECT ""FinishedFoodID"",""BatchOutputQty""
            FROM public.""FoodRecipe"" WHERE ""RecipeID""=@id AND ""IsActive""=TRUE FOR UPDATE", c, tx))
                {
                    recipe.Parameters.AddWithValue("@id", m.RecipeID);
                    await using var rr = await recipe.ExecuteReaderAsync();
                    if (!await rr.ReadAsync()) return NotFound("Active recipe not found.");
                    finishedFoodId = Convert.ToInt32(rr[0]); batchOutput = Convert.ToDecimal(rr[1]);
                }
                var ingredients = new List<(int ItemID, string ItemType, decimal Qty)>();
                await using (var q = new NpgsqlCommand(@"SELECT d.""RawMaterialID"",i.""ItemType"",d.""Quantity""
            FROM public.""FoodRecipeDetail"" d JOIN public.""Item"" i ON i.""ItemID""=d.""RawMaterialID""
            WHERE d.""RecipeID""=@id AND i.""IsActive""=TRUE ORDER BY d.""RawMaterialID""", c, tx))
                {
                    q.Parameters.AddWithValue("@id", m.RecipeID);
                    await using var rd = await q.ExecuteReaderAsync();
                    while (await rd.ReadAsync()) ingredients.Add((Convert.ToInt32(rd[0]), rd.GetString(1), Convert.ToDecimal(rd[2])));
                }
                if (ingredients.Count == 0) return BadRequest("Recipe has no active ingredients.");
                var factor = m.ProducedQty / batchOutput;
                var requirements = ingredients.Select(x => (x.ItemID, x.ItemType, Qty: decimal.Round(x.Qty * factor, 3))).ToList();

                // Lock all ingredient/output item rows in ID order to reduce deadlock risk.
                foreach (var id in requirements.Select(x => x.ItemID).Append(finishedFoodId).Distinct().OrderBy(x => x))
                {
                    await using var lk = new NpgsqlCommand("SELECT \"ItemID\" FROM public.\"Item\" WHERE \"ItemID\"=@id AND \"IsActive\"=TRUE FOR UPDATE", c, tx);
                    lk.Parameters.AddWithValue("@id", id);
                    if (await lk.ExecuteScalarAsync() == null) throw new InvalidOperationException($"Item {id} is inactive or missing.");
                }
                foreach (var req in requirements) await EnsureFoodStockAvailable(c, tx, req.ItemID, req.Qty);

                long productionId;
                await using (var h = new NpgsqlCommand(@"INSERT INTO public.""FoodProduction""
            (""ProductionDate"",""RecipeID"",""FinishedFoodID"",""ProducedQty"",""Remarks"")
            VALUES (@date,@recipe,@food,@qty,@remarks) RETURNING ""ProductionID""", c, tx))
                {
                    h.Parameters.AddWithValue("@date", NpgsqlDbType.Date, m.ProductionDate.Date); h.Parameters.AddWithValue("@recipe", m.RecipeID);
                    h.Parameters.AddWithValue("@food", finishedFoodId); h.Parameters.AddWithValue("@qty", m.ProducedQty);
                    h.Parameters.AddWithValue("@remarks", (object?)m.Remarks?.Trim() ?? DBNull.Value);
                    productionId = Convert.ToInt64(await h.ExecuteScalarAsync());
                }
                foreach (var req in requirements)
                {
                    long detailId;
                    await using (var d = new NpgsqlCommand(@"INSERT INTO public.""FoodProductionDetail""
                (""ProductionID"",""ItemID"",""ItemType"",""RequiredQty"") VALUES (@production,@item,@type,@qty)
                RETURNING ""ProductionDetailID""", c, tx))
                    {
                        d.Parameters.AddWithValue("@production", productionId); d.Parameters.AddWithValue("@item", req.ItemID);
                        d.Parameters.AddWithValue("@type", req.ItemType); d.Parameters.AddWithValue("@qty", req.Qty);
                        detailId = Convert.ToInt64(await d.ExecuteScalarAsync());
                    }
                    await using var l = new NpgsqlCommand(@"INSERT INTO public.""StockTransaction""
                (""TransactionDate"",""ItemID"",""TransactionType"",""SourceType"",""SourceID"",""SourceDetailID"",""QtyOut"")
                VALUES (@date,@item,'PRODUCTION_CONSUMPTION','FOOD_PRODUCTION',@source,@detail,@qty)", c, tx);
                    l.Parameters.AddWithValue("@date", NpgsqlDbType.Date, m.ProductionDate.Date); l.Parameters.AddWithValue("@item", req.ItemID);
                    l.Parameters.AddWithValue("@source", productionId); l.Parameters.AddWithValue("@detail", detailId); l.Parameters.AddWithValue("@qty", req.Qty);
                    await l.ExecuteNonQueryAsync();
                }
                await using (var outLedger = new NpgsqlCommand(@"INSERT INTO public.""StockTransaction""
            (""TransactionDate"",""ItemID"",""TransactionType"",""SourceType"",""SourceID"",""QtyIn"")
            VALUES (@date,@item,'PRODUCTION_OUTPUT','FOOD_PRODUCTION',@source,@qty)", c, tx))
                {
                    outLedger.Parameters.AddWithValue("@date", NpgsqlDbType.Date, m.ProductionDate.Date); outLedger.Parameters.AddWithValue("@item", finishedFoodId);
                    outLedger.Parameters.AddWithValue("@source", productionId); outLedger.Parameters.AddWithValue("@qty", m.ProducedQty);
                    await outLedger.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
                return Ok(new { message = "Production saved; ingredients consumed and finished food added to stock.", productionID = productionId });
            }
            catch (Exception ex) { await tx.RollbackAsync(); return BadRequest(ex.Message); }
        }

        private static async Task EnsureFoodStockAvailable(NpgsqlConnection c, NpgsqlTransaction tx, int itemId, decimal required)
        {
            await using var q = new NpgsqlCommand(@"SELECT COALESCE(SUM(""QtyIn""-""QtyOut""),0)
        FROM public.""StockTransaction"" WHERE ""ItemID""=@id", c, tx);
            q.Parameters.AddWithValue("@id", itemId);
            var balance = Convert.ToDecimal(await q.ExecuteScalarAsync());
            if (balance < required) throw new InvalidOperationException($"Insufficient stock for ItemID {itemId}. Available: {balance:0.###}, required: {required:0.###}.");
        }

        [HttpGet("food/stock/report")]
        public async Task<IActionResult> GetFoodStockReport([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            if (fromDate == default || toDate == default || toDate.Date < fromDate.Date)
                return BadRequest("A valid From Date and To Date are required.");
            try
            {
                var rows = new List<object>();
                await using var c = new NpgsqlConnection(connectionString); await c.OpenAsync();
                const string sql = @"SELECT i.""ItemID"",i.""ItemName"",i.""ItemType"",i.""BaseUnit"",
            COALESCE(SUM(CASE WHEN s.""TransactionDate"" < @from THEN s.""QtyIn""-s.""QtyOut"" ELSE 0 END),0) AS opening,
            COALESCE(SUM(CASE WHEN s.""TransactionDate"" BETWEEN @from AND @to AND s.""TransactionType""='PURCHASE' THEN s.""QtyIn"" ELSE 0 END),0) AS purchased,
            COALESCE(SUM(CASE WHEN s.""TransactionDate"" BETWEEN @from AND @to AND s.""TransactionType""='PRODUCTION_OUTPUT' THEN s.""QtyIn"" ELSE 0 END),0) AS produced,
            COALESCE(SUM(CASE WHEN s.""TransactionDate"" BETWEEN @from AND @to AND s.""TransactionType""='PRODUCTION_CONSUMPTION' THEN s.""QtyOut"" ELSE 0 END),0) AS consumed,
            COALESCE(SUM(CASE WHEN s.""TransactionDate"" BETWEEN @from AND @to AND s.""TransactionType""='ISSUE' THEN s.""QtyOut"" ELSE 0 END),0) AS issued,
            COALESCE(SUM(CASE WHEN s.""TransactionDate"" BETWEEN @from AND @to AND s.""TransactionType""='WASTE' THEN s.""QtyOut"" ELSE 0 END),0) AS wasted,
            COALESCE(SUM(CASE WHEN s.""TransactionDate"" <= @to THEN s.""QtyIn""-s.""QtyOut"" ELSE 0 END),0) AS closing
            FROM public.""Item"" i LEFT JOIN public.""StockTransaction"" s ON s.""ItemID""=i.""ItemID""
            GROUP BY i.""ItemID"",i.""ItemName"",i.""ItemType"",i.""BaseUnit"" ORDER BY i.""ItemType"",i.""ItemName""";
                await using var q = new NpgsqlCommand(sql, c);
                q.Parameters.AddWithValue("@from", NpgsqlDbType.Date, fromDate.Date); q.Parameters.AddWithValue("@to", NpgsqlDbType.Date, toDate.Date);
                await using var r = await q.ExecuteReaderAsync();
                while (await r.ReadAsync()) rows.Add(new
                {
                    itemID = Convert.ToInt32(r["ItemID"]),
                    itemName = r["ItemName"].ToString(),
                    itemType = r["ItemType"].ToString(),
                    baseUnit = r["BaseUnit"].ToString(),
                    opening = Convert.ToDecimal(r["opening"]),
                    purchased = Convert.ToDecimal(r["purchased"]),
                    produced = Convert.ToDecimal(r["produced"]),
                    consumed = Convert.ToDecimal(r["consumed"]),
                    issued = Convert.ToDecimal(r["issued"]),
                    waste = Convert.ToDecimal(r["wasted"]),
                    closing = Convert.ToDecimal(r["closing"])
                });
                return Ok(new { fromDate = fromDate.ToString("yyyy-MM-dd"), toDate = toDate.ToString("yyyy-MM-dd"), items = rows });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

    }
}
