using CloudNative.CloudEvents;
using Google.Cloud.Firestore;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Vision.V1;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using System.Text.Json;

namespace process_menu_upload_function
{
    public class Function : ICloudEventFunction<MessagePublishedData>
    {
        private ImageAnnotatorClient visionClient;
        private FirestoreDb db;

        public Function()
        {
            visionClient = ImageAnnotatorClient.Create();

            db = new FirestoreDbBuilder
            {
                ProjectId = "menu-mania-demo",
                DatabaseId = "menu-mania-db"
            }.Build();
        }

        public async Task HandleAsync(CloudEvent cloudEvent, MessagePublishedData data, CancellationToken cancellationToken)
        {
            try
            {
                if (data == null || data.Message == null || data.Message.Data == null)
                {
                    Console.WriteLine("Message data was null");
                    return;
                }

                string json = data.Message.Data.ToStringUtf8();

                var payload = JsonSerializer.Deserialize<MenuUploadMessage>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payload == null)
                {
                    Console.WriteLine("Payload could not be read");
                    return;
                }

                if (payload.BucketName == "" || payload.FileName == "" ||
                    payload.RestaurantId == "" || payload.MenuId == "")
                {
                    Console.WriteLine("Missing fields in payload");
                    return;
                }

                string filePath = "gs://" + payload.BucketName + "/" + payload.FileName;

                var image = Image.FromUri(filePath);
                var textResult = await visionClient.DetectTextAsync(image);

                string ocrText = "";

                if (textResult != null && textResult.Count > 0)
                {
                    ocrText = textResult[0].Description ?? "";
                }

                var restaurantRef = db.Collection("restaurants").Document(payload.RestaurantId);
                var menuRef = restaurantRef.Collection("menus").Document(payload.MenuId);

                await UpdateMenu(menuRef, ocrText);

                await restaurantRef.SetAsync(new Dictionary<string, object>
                {
                    { "Status", "pending" }
                }, SetOptions.MergeAll);

                if (!string.IsNullOrWhiteSpace(payload.ImageId))
                {
                    var imageRef = menuRef.Collection("images").Document(payload.ImageId);

                    await imageRef.SetAsync(new Dictionary<string, object>
                    {
                        { "BucketName", payload.BucketName },
                        { "FileName", payload.FileName },
                        { "StoragePath", filePath },
                        { "Status", "processed" }
                    }, SetOptions.MergeAll);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error happened: " + ex.Message);
                throw;
            }
        }

        private async Task UpdateMenu(DocumentReference menuRef, string newText)
        {
            await db.RunTransactionAsync(async transaction =>
            {
                var snapshot = await transaction.GetSnapshotAsync(menuRef);

                string oldText = "";

                if (snapshot.Exists && snapshot.ContainsField("OcrText"))
                {
                    oldText = snapshot.GetValue<string>("OcrText") ?? "";
                }

                string finalText;

                if (oldText == "")
                {
                    finalText = newText;
                }
                else
                {
                    finalText = oldText + "\n" + newText;
                }

                transaction.Set(menuRef, new Dictionary<string, object>
                {
                    { "OcrText", finalText },
                    { "Status", "pending" }
                }, SetOptions.MergeAll);
            });
        }
    }

    public class MenuUploadMessage
    {
        public string BucketName { get; set; } = "";
        public string FileName { get; set; } = "";
        public string RestaurantId { get; set; } = "";
        public string MenuId { get; set; } = "";
        public string? ImageId { get; set; }
    }
}