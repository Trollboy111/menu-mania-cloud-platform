namespace MenuManiaCloudPlatform.Models
{
    public class TranslateRequest
    {
        public string RestaurantId { get; set; } = string.Empty;
        public string MenuId { get; set; } = string.Empty;
        public string MenuItemId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
    }
}
