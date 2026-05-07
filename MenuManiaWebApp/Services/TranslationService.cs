using System.Net.Http.Headers;
using System.Net.Http.Json;
using Google.Apis.Auth.OAuth2;

namespace MenuManiaCloudPlatform.Services
{
    public class TranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly CacheService _cacheService;
        private readonly ILogger<TranslationService> _logger;

        public TranslationService(
            HttpClient httpClient,
            IConfiguration configuration,
            CacheService cacheService,
            ILogger<TranslationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<string?> GetOrCreateTranslationAsync(
            string restaurantId,
            string menuId,
            string menuItemId,
            string text,
            string targetLanguage)
        {
            var cached = await _cacheService.GetTranslationAsync(
                restaurantId,
                menuId,
                menuItemId,
                targetLanguage);

            if (cached != null)
            {
                _logger.LogInformation(
                    "Translation cache hit for restaurant {RestaurantId}, menu {MenuId}, item {MenuItemId}, lang {Lang}",
                    restaurantId, menuId, menuItemId, targetLanguage);

                return cached.TranslatedText;
            }

            _logger.LogInformation(
                "Translation cache miss for restaurant {RestaurantId}, menu {MenuId}, item {MenuItemId}, lang {Lang}",
                restaurantId, menuId, menuItemId, targetLanguage);

            string? translatedText = await CallTranslateFunctionAsync(text, targetLanguage);

            if (string.IsNullOrWhiteSpace(translatedText))
            {
                return null;
            }

            var translation = new CachedTranslation
            {
                RestaurantId = restaurantId,
                MenuId = menuId,
                MenuItemId = menuItemId,
                OriginalText = text,
                TranslatedText = translatedText,
                TargetLanguage = targetLanguage,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _cacheService.SetTranslationAsync(translation);

            return translatedText;
        }

        private async Task<string?> CallTranslateFunctionAsync(string text, string targetLanguage)
        {
            string? functionUrl = _configuration["GoogleCloud:TranslateFunctionUrl"];

            if (string.IsNullOrWhiteSpace(functionUrl))
            {
                throw new InvalidOperationException("Translate function URL is missing in configuration.");
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, functionUrl)
                {
                    Content = JsonContent.Create(new
                    {
                        text,
                        targetLanguage
                    })
                };

                bool useAuth = _configuration.GetValue<bool>("GoogleCloud:UseAuthenticatedFunction");

                if (useAuth)
                {
                    string? token = await GetIdentityTokenAsync(functionUrl);

                    if (string.IsNullOrWhiteSpace(token))
                    {
                        throw new InvalidOperationException("Could not obtain identity token for Cloud Function.");
                    }

                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();

                    _logger.LogError(
                        "Translation function failed. Status: {StatusCode}, Body: {Body}",
                        response.StatusCode, errorBody);

                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<TranslationResponse>();
                return result?.TranslatedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling translation function.");
                return null;
            }
        }

        private async Task<string?> GetIdentityTokenAsync(string audience)
        {
            try
            {
                GoogleCredential credential = await GoogleCredential.GetApplicationDefaultAsync();

                if (credential.UnderlyingCredential is ServiceAccountCredential
                    || credential.UnderlyingCredential is ComputeCredential
                    || credential.UnderlyingCredential is ImpersonatedCredential)
                {
                    var oidcToken = await credential.GetOidcTokenAsync(
                        OidcTokenOptions.FromTargetAudience(audience));

                    return await oidcToken.GetAccessTokenAsync();
                }

                _logger.LogWarning(
                    "ADC is not a supported OIDC token provider. Current credential type: {CredentialType}",
                    credential.UnderlyingCredential.GetType().Name);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to obtain identity token.");
                return null;
            }
        }

        private class TranslationResponse
        {
            public string? TranslatedText { get; set; }
        }
    }
}