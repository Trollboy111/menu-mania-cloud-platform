using Google.Cloud.Firestore;
using MenuManiaCloudPlatform.Models;

namespace MenuManiaCloudPlatform.Services
{
    public class FirestoreService
    {
        private readonly ILogger<FirestoreService> _logger;
        private readonly FirestoreDb _firestoreDb;

        public FirestoreService(ILogger<FirestoreService> logger, IConfiguration configuration)
        {
            _logger = logger;

            string? projectId = configuration["GoogleCloud:ProjectId"];

            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new InvalidOperationException("ProjectId is missing.");
            }

            string databaseId = configuration["GoogleCloud:FirestoreDatabaseId"] ?? "(default)";

            _firestoreDb = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                DatabaseId = databaseId
            }.Build();
        }

        public async Task<string> CreateRestaurantAsync(Restaurant restaurant)
        {
            DocumentReference docRef = _firestoreDb.Collection("restaurants").Document();

            restaurant.Id = docRef.Id;

            await docRef.SetAsync(restaurant);

            _logger.LogInformation("Restaurant {Id} created successfully.", restaurant.Id);

            return restaurant.Id!;
        }

        public async Task<Restaurant?> GetRestaurantByNameAsync(string restaurantName)
        {
            if (string.IsNullOrWhiteSpace(restaurantName))
            {
                throw new ArgumentException("Restaurant name cannot be empty.");
            }

            restaurantName = restaurantName.Trim();

            Query query = _firestoreDb
                .Collection("restaurants")
                .WhereEqualTo("Name", restaurantName)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                _logger.LogInformation("No restaurant found with name {RestaurantName}", restaurantName);
                return null;
            }

            DocumentSnapshot document = snapshot.Documents[0];
            Restaurant restaurant = document.ConvertTo<Restaurant>();
            restaurant.Id = document.Id;

            _logger.LogInformation(
                "Restaurant {RestaurantName} found with id {RestaurantId}",
                restaurantName,
                restaurant.Id);

            return restaurant;
        }

        public async Task<string> GetOrCreateRestaurantAsync(string restaurantName)
        {
            if (string.IsNullOrWhiteSpace(restaurantName))
            {
                throw new ArgumentException("Restaurant name cannot be empty.");
            }

            restaurantName = restaurantName.Trim();

            Restaurant? existingRestaurant = await GetRestaurantByNameAsync(restaurantName);

            if (existingRestaurant != null)
            {
                return existingRestaurant.Id!;
            }

            Restaurant newRestaurant = new Restaurant
            {
                Name = restaurantName,
                Status = "pending"
            };

            return await CreateRestaurantAsync(newRestaurant);
        }

        public async Task<string> CreateMenuAsync(string restaurantId, MenuDocument menu)
        {
            DocumentReference docRef = _firestoreDb
                .Collection("restaurants")
                .Document(restaurantId)
                .Collection("menus")
                .Document();

            menu.Id = docRef.Id;

            if (string.IsNullOrWhiteSpace(menu.Status))
            {
                menu.Status = "pending";
            }

            menu.CreatedAt = Timestamp.GetCurrentTimestamp();

            if (menu.Items == null)
            {
                menu.Items = new List<MenuDish>();
            }

            await docRef.SetAsync(menu);

            _logger.LogInformation(
                "Menu {MenuId} created for restaurant {RestaurantId}",
                menu.Id,
                restaurantId);

            return menu.Id!;
        }

        public async Task<string> AddImageAsync(string restaurantId, string menuId, MenuImage image)
        {
            DocumentReference docRef = _firestoreDb
                .Collection("restaurants")
                .Document(restaurantId)
                .Collection("menus")
                .Document(menuId)
                .Collection("images")
                .Document();

            image.Id = docRef.Id;

            await docRef.SetAsync(image);

            _logger.LogInformation("Image {ImageId} added to menu {MenuId}", image.Id, menuId);

            return image.Id!;
        }

        public async Task<MenuDocument?> GetActiveMenuAsync(string restaurantId)
        {
            QuerySnapshot snapshot = await _firestoreDb
                .Collection("restaurants")
                .Document(restaurantId)
                .Collection("menus")
                .GetSnapshotAsync();

            MenuDocument? latestActiveMenu = null;

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                MenuDocument menu = doc.ConvertTo<MenuDocument>();
                menu.Id = doc.Id;

                if (!menu.IsActive)
                {
                    continue;
                }

                if (latestActiveMenu == null || menu.CreatedAt > latestActiveMenu.CreatedAt)
                {
                    latestActiveMenu = menu;
                }
            }

            return latestActiveMenu;
        }

        public async Task DeactivateMenusAsync(string restaurantId)
        {
            QuerySnapshot snapshot = await _firestoreDb
                .Collection("restaurants")
                .Document(restaurantId)
                .Collection("menus")
                .WhereEqualTo("IsActive", true)
                .GetSnapshotAsync();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                await doc.Reference.UpdateAsync(new Dictionary<string, object>
                {
                    { "IsActive", false }
                });
            }

            _logger.LogInformation("All active menus deactivated for restaurant {RestaurantId}", restaurantId);
        }

        public async Task<List<MenuDocument>> GetMenusForRestaurantAsync(string restaurantId)
        {
            QuerySnapshot snapshot = await _firestoreDb
                .Collection("restaurants")
                .Document(restaurantId)
                .Collection("menus")
                .GetSnapshotAsync();

            List<MenuDocument> menus = new List<MenuDocument>();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                MenuDocument menu = doc.ConvertTo<MenuDocument>();
                menu.Id = doc.Id;
                menus.Add(menu);
            }

            return menus
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }

        public async Task<List<MenuImage>> GetImagesForMenuAsync(string restaurantId, string menuId)
        {
            QuerySnapshot snapshot = await _firestoreDb
                .Collection("restaurants")
                .Document(restaurantId)
                .Collection("menus")
                .Document(menuId)
                .Collection("images")
                .GetSnapshotAsync();

            List<MenuImage> images = new List<MenuImage>();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                MenuImage image = doc.ConvertTo<MenuImage>();
                image.Id = doc.Id;
                images.Add(image);
            }

            return images;
        }

        public async Task<List<CatalogueItem>> GetCatalogueItemsAsync(string? searchTerm = null, string? sortOrder = null)
        {
            List<CatalogueItem> results = new List<CatalogueItem>();

            QuerySnapshot restaurantsSnapshot = await _firestoreDb
                .Collection("restaurants")
                .GetSnapshotAsync();

            foreach (DocumentSnapshot restaurantDoc in restaurantsSnapshot.Documents)
            {
                string restaurantId = restaurantDoc.Id;
                string restaurantName = "Unknown Restaurant";

                Dictionary<string, object> restaurantData = restaurantDoc.ToDictionary();

                if (restaurantData.ContainsKey("Name") && restaurantData["Name"] != null)
                {
                    restaurantName = restaurantData["Name"].ToString()!;
                }

                QuerySnapshot menusSnapshot = await restaurantDoc.Reference
                    .Collection("menus")
                    .WhereEqualTo("IsActive", true)
                    .GetSnapshotAsync();

                foreach (DocumentSnapshot menuDoc in menusSnapshot.Documents)
                {
                    MenuDocument menu = menuDoc.ConvertTo<MenuDocument>();
                    menu.Id = menuDoc.Id;

                    if (menu.Status != "completed")
                    {
                        continue;
                    }

                    foreach (MenuDish dish in menu.Items)
                    {
                        if (string.IsNullOrWhiteSpace(dish.Name))
                        {
                            continue;
                        }

                        CatalogueItem item = new CatalogueItem
                        {
                            RestaurantId = restaurantId,
                            MenuId = menu.Id!,
                            MenuItemId = dish.Id,
                            RestaurantName = restaurantName,
                            ItemName = dish.Name,
                            Price = dish.Price,
                            Status = menu.Status
                        };

                        results.Add(item);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                results = results
                    .Where(x =>
                        x.ItemName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        x.RestaurantName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (sortOrder == "desc")
            {
                results = results.OrderByDescending(x => x.Price).ToList();
            }
            else
            {
                results = results.OrderBy(x => x.Price).ToList();
            }

            return results;
        }
    }
}