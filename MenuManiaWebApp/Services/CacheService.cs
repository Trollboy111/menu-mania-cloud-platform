using StackExchange.Redis;
using System.Text.Json;

namespace MenuManiaCloudPlatform.Services
{
    public class CacheService
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public CacheService(IConfiguration configuration)
        {
            var redisConnection = configuration["Redis:ConnectionString"];

            if (string.IsNullOrWhiteSpace(redisConnection))
            {
                throw new InvalidOperationException("Redis connection string is missing.");
            }

            _redis = ConnectionMultiplexer.Connect(redisConnection);
            _db = _redis.GetDatabase();
        }

        private static string BuildKey(string restaurantId, string menuId, string menuItemId, string lang)
        {
            return $"translation:{restaurantId}:{menuId}:{menuItemId}:{lang}";
        }

        public async Task<CachedTranslation?> GetTranslationAsync(
            string restaurantId,
            string menuId,
            string menuItemId,
            string lang)
        {
            string key = BuildKey(restaurantId, menuId, menuItemId, lang);
            var value = await _db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<CachedTranslation>(value!);
        }

        public async Task SetTranslationAsync(CachedTranslation translation)
        {
            string key = BuildKey(
                translation.RestaurantId,
                translation.MenuId,
                translation.MenuItemId,
                translation.TargetLanguage);

            string json = JsonSerializer.Serialize(translation);

            await _db.StringSetAsync(key, json, TimeSpan.FromHours(24));
        }

        public async Task InvalidateMenuAsync(string restaurantId, string menuId)
        {
            var endpoints = _redis.GetEndPoints();

            if (endpoints.Length == 0)
            {
                return;
            }

            var server = _redis.GetServer(endpoints.First());
            string pattern = $"translation:{restaurantId}:{menuId}:*";

            foreach (var key in server.Keys(pattern: pattern))
            {
                await _db.KeyDeleteAsync(key);
            }
        }

        public async Task InvalidateRestaurantAsync(string restaurantId)
        {
            var endpoints = _redis.GetEndPoints();

            if (endpoints.Length == 0)
            {
                return;
            }

            var server = _redis.GetServer(endpoints.First());
            string pattern = $"translation:{restaurantId}:*";

            foreach (var key in server.Keys(pattern: pattern))
            {
                await _db.KeyDeleteAsync(key);
            }
        }
    }
}