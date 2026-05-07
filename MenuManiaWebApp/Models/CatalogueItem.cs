namespace MenuManiaCloudPlatform.Models
{
    public class CatalogueItem
    {
        public string RestaurantId { get; set; } = "";
        public string MenuId { get; set; } = "";
        public string MenuItemId { get; set; } = "";
        public string RestaurantName { get; set; } = "";
        public string ItemName { get; set; } = "";
        public double Price { get; set; }
        public string Status { get; set; } = "";
    }
}