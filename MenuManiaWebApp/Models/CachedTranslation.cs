namespace MenuManiaCloudPlatform.Services
{
    public class CachedTranslation
    {
        public string RestaurantId { get; set; } = "";
        public string MenuId { get; set; } = "";
        public string MenuItemId { get; set; } = "";
        public string OriginalText { get; set; } = "";
        public string TranslatedText { get; set; } = "";
        public string TargetLanguage { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
    }
}