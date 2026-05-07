using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuManiaCloudPlatform.Models;
using MenuManiaCloudPlatform.Services;
using System.Security.Claims;

namespace MenuManiaCloudPlatform.Controllers
{
    public class MenuController : Controller
    {
        private readonly StorageService _storageService;
        private readonly FirestoreService _firestoreService;
        private readonly PubSubService _pubSubService;
        private readonly TranslationService _translationService;
        private readonly CacheService _cacheService;

        public MenuController(
            StorageService storageService,
            FirestoreService firestoreService,
            PubSubService pubSubService,
            TranslationService translationService,
            CacheService cacheService)
        {
            _storageService = storageService;
            _firestoreService = firestoreService;
            _pubSubService = pubSubService;
            _translationService = translationService;
            _cacheService = cacheService;
        }

        [Authorize]
        public IActionResult Index()
        {
            return RedirectToAction("Catalogue");
        }

        [Authorize]
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> StartUpload(string restaurantName)
        {
            if (string.IsNullOrWhiteSpace(restaurantName))
            {
                return BadRequest(new { message = "Restaurant name is required." });
            }

            restaurantName = restaurantName.Trim();

            string restaurantId = await _firestoreService.GetOrCreateRestaurantAsync(restaurantName);
            var previousActiveMenu = await _firestoreService.GetActiveMenuAsync(restaurantId);

            await _firestoreService.DeactivateMenusAsync(restaurantId);
            await _cacheService.InvalidateRestaurantAsync(restaurantId);

            var menu = new MenuDocument
            {
                Items = new List<MenuDish>(),
                OcrText = "",
                Status = "pending",
                CreatedAt = Timestamp.GetCurrentTimestamp(),
                IsActive = true,
                PreviousMenuId = previousActiveMenu?.Id
            };

            string menuId = await _firestoreService.CreateMenuAsync(restaurantId, menu);

            return Json(new
            {
                restaurantId,
                menuId
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UploadSingle(string restaurantId, string menuId, IFormFile file)
        {
            if (string.IsNullOrWhiteSpace(restaurantId))
            {
                return BadRequest(new { message = "Restaurant id is required." });
            }

            if (string.IsNullOrWhiteSpace(menuId))
            {
                return BadRequest(new { message = "Menu id is required." });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Please select a valid file." });
            }

            string userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "unknown";

            string objectName = await _storageService.UploadFileAsync(file, restaurantId);
            string bucketName = _storageService.GetBucketName();

            var image = new MenuImage
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                BucketName = bucketName,
                ObjectName = objectName,
                StoragePath = $"gs://{bucketName}/{objectName}",
                UploadedByEmail = userEmail,
                UploadedAt = Timestamp.GetCurrentTimestamp()
            };

            string imageId = await _firestoreService.AddImageAsync(restaurantId, menuId, image);

            await _pubSubService.PublishAsync(
                restaurantId,
                menuId,
                imageId,
                bucketName,
                objectName
            );

            return Ok(new
            {
                message = "File uploaded successfully.",
                imageId = imageId
            });
        }

        [Authorize]
        public async Task<IActionResult> Catalogue(string? searchTerm, string? sortOrder = "asc")
        {
            var items = await _firestoreService.GetCatalogueItemsAsync(searchTerm, sortOrder);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortOrder = sortOrder;

            return View(items);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> TranslateItem([FromBody] TranslateRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request is required." });
            }

            if (string.IsNullOrWhiteSpace(request.RestaurantId) ||
                string.IsNullOrWhiteSpace(request.MenuId) ||
                string.IsNullOrWhiteSpace(request.MenuItemId) ||
                string.IsNullOrWhiteSpace(request.Text) ||
                string.IsNullOrWhiteSpace(request.TargetLanguage))
            {
                return BadRequest(new
                {
                    error = "RestaurantId, MenuId, MenuItemId, text and target language are required."
                });
            }

            string? translatedText = await _translationService.GetOrCreateTranslationAsync(
                request.RestaurantId,
                request.MenuId,
                request.MenuItemId,
                request.Text,
                request.TargetLanguage
            );

            if (string.IsNullOrWhiteSpace(translatedText))
            {
                return StatusCode(500, new { error = "Translation failed." });
            }

            return Json(new { translatedText });
        }
    }
}