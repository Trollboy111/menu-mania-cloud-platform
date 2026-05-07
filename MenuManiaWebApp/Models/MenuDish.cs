using Google.Cloud.Firestore;

namespace MenuManiaCloudPlatform.Models
{

    [FirestoreData]
    public class MenuDish
    {
        [FirestoreProperty]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [FirestoreProperty]
        public string Name { get; set; } = "";

        [FirestoreProperty]
        public double Price { get; set; }
    }
}
