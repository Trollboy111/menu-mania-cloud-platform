using Google.Cloud.Firestore;

namespace MenuManiaCloudPlatform.Models
{
    [FirestoreData]
    public class MenuImage
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string FileName { get; set; } = "";

        [FirestoreProperty]
        public string ContentType { get; set; } = "";

        [FirestoreProperty]
        public string BucketName { get; set; } = "";

        [FirestoreProperty]
        public string ObjectName { get; set; } = "";

        [FirestoreProperty]
        public string StoragePath { get; set; } = "";

        [FirestoreProperty]
        public string UploadedByEmail { get; set; } = "";

        [FirestoreProperty]
        public Timestamp UploadedAt { get; set; } = Timestamp.GetCurrentTimestamp();
    }
}