using Google.Cloud.Firestore;
using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace clean_pending_menus_function
{
    public class Function : IHttpFunction
    {
        public async Task HandleAsync(HttpContext context)
        {
            int processedCount = 0;

            try
            {
                var db = new FirestoreDbBuilder
                {
                    ProjectId = "menu-mania-demo",
                    DatabaseId = "menu-mania-db"
                }.Build();

                var menus = await db.CollectionGroup("menus")
                    .WhereEqualTo("Status", "pending")
                    .GetSnapshotAsync();

                foreach (var menu in menus.Documents)
                {
                    if (!menu.Exists)
                    {
                        continue;
                    }

                    string ocrText = "";

                    if (menu.ContainsField("OcrText"))
                    {
                        ocrText = menu.GetValue<string>("OcrText");
                    }

                    var menuItems = new List<Dictionary<string, object>>();

                    string pattern = @"([A-Za-z][A-Za-z\s&'\-/()]{2,}?)\s*€?\s*(\d{1,2}[.,]\d{2})";

                    var matches = Regex.Matches(ocrText, pattern);

                    foreach (Match match in matches)
                    {
                        string itemName = match.Groups[1].Value.Trim();
                        string priceText = match.Groups[2].Value.Replace(",", ".");

                        itemName = Regex.Replace(itemName, @"[^a-zA-Z\s&'\-/()]", "").Trim();

                        if (itemName.Length < 3)
                        {
                            continue;
                        }

                        if (double.TryParse(priceText, out double price))
                        {
                            menuItems.Add(new Dictionary<string, object>
                            {
                                { "Name", itemName },
                                { "Price", price }
                            });
                        }
                    }

                    await menu.Reference.UpdateAsync(new Dictionary<string, object>
                    {
                        { "Items", menuItems },
                        { "Status", "completed" },
                        { "LastCleaned", FieldValue.ServerTimestamp }
                    });

                    var restaurantRef = menu.Reference.Parent.Parent;

                    if (restaurantRef != null)
                    {
                        await restaurantRef.UpdateAsync(new Dictionary<string, object>
                        {
                            { "Status", "completed" }
                        });
                    }

                    processedCount++;
                }

                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("Processed " + processedCount + " menus.");
            }
            catch (System.Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Error: " + ex.Message);
            }
        }
    }
}