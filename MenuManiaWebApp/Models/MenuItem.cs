using Google.Cloud.Firestore;

namespace MenuManiaCloudPlatform.Models
{
    [FirestoreData]
    public class MenuDocument
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty]
        public List<MenuDish> Items { get; set; } = new List<MenuDish>();

        [FirestoreProperty]
        public string OcrText { get; set; } = "";

        [FirestoreProperty]
        public string Status { get; set; } = "pending";

        [FirestoreProperty]
        public Timestamp CreatedAt { get; set; } = Timestamp.GetCurrentTimestamp();

        [FirestoreProperty]
        public bool IsActive { get; set; } = true;

        [FirestoreProperty]
        public string? PreviousMenuId { get; set; }
    }
}