using Google.Cloud.Firestore;

namespace MenuManiaCloudPlatform.Models
{
    [FirestoreData]
    public class Restaurant
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string Name { get; set; } = "";

        [FirestoreProperty]
        public string Status { get; set; } = "pending";
    }
}