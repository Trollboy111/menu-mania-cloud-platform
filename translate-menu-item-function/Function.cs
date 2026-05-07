using Google.Cloud.Functions.Framework;
using Google.Cloud.Translation.V2;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace translate_menu_item_function
{
    public class Function : IHttpFunction
    {
        private TranslationClient translateClient;

        public Function()
        {
            translateClient = TranslationClient.Create();
        }

        public async Task HandleAsync(HttpContext context)
        {
            if (context.Request.Method != "POST")
            {
                context.Response.StatusCode = 405;
                context.Response.ContentType = "application/json";

                var errorResponse = new { error = "Only POST requests are allowed" };
                await JsonSerializer.SerializeAsync(context.Response.Body, errorResponse);
                return;
            }

            try
            {
                var request = await JsonSerializer.DeserializeAsync<TranslateRequest>(
                    context.Request.Body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (request == null || request.Text == "" || request.TargetLanguage == "")
                {
                    context.Response.StatusCode = 400;
                    context.Response.ContentType = "application/json";

                    var errorResponse = new { error = "Text or language is missing" };
                    await JsonSerializer.SerializeAsync(context.Response.Body, errorResponse);
                    return;
                }

                var translation = translateClient.TranslateText(
                    request.Text,
                    request.TargetLanguage
                );

                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    translatedText = translation.TranslatedText
                };

                await JsonSerializer.SerializeAsync(context.Response.Body, response);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    error = "Something went wrong: " + ex.Message
                };

                await JsonSerializer.SerializeAsync(context.Response.Body, errorResponse);
            }
        }
    }

    public class TranslateRequest
    {
        public string Text { get; set; } = "";
        public string TargetLanguage { get; set; } = "";
    }
}