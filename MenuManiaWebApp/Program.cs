using Google.Cloud.SecretManager.V1;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using MenuManiaCloudPlatform.Services;
using System.Security.Claims;

namespace MenuManiaCloudPlatform
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            string projectId = "menu-mania-demo";

            var secretClient = SecretManagerServiceClient.Create();

            string GetSecret(string secretName)
            {
                var secretVersionName = new SecretVersionName(projectId, secretName, "latest");
                var result = secretClient.AccessSecretVersion(secretVersionName);
                return result.Payload.Data.ToStringUtf8();
            }

            var googleClientId = GetSecret("google-client-id");
            var googleClientSecret = GetSecret("google-client-secret");

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
            })
            .AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;

                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.Events.OnCreatingTicket = context =>
                {
                    var email = context.User.GetProperty("email").GetString();
                    var picture = context.User.GetProperty("picture").GetString();
                    var name = context.User.GetProperty("name").GetString();

                    if (!string.IsNullOrEmpty(email))
                        context.Identity?.AddClaim(new Claim(ClaimTypes.Email, email));

                    if (!string.IsNullOrEmpty(name))
                        context.Identity?.AddClaim(new Claim(ClaimTypes.Name, name));

                    if (!string.IsNullOrEmpty(picture))
                        context.Identity?.AddClaim(new Claim("picture", picture));

                    return Task.CompletedTask;
                };
            });

            builder.Services.AddAuthorization();
            builder.Services.AddControllersWithViews();
            builder.Services.AddSingleton<StorageService>();
            builder.Services.AddSingleton<FirestoreService>();
            builder.Services.AddScoped<PubSubService>();
            builder.Services.AddHttpClient<TranslationService>();
            builder.Services.AddSingleton<CacheService>();

            var app = builder.Build();

            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Menu}/{action=Index}/{id?}");

            app.Run();
        }
    }
}