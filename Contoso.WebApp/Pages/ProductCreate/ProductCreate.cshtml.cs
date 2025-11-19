using Contoso.WebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

public class ProductCreateModel : PageModel
{

        public bool IsAdmin { get; set; }

        public string ImageUrl { get; set; }

        [BindProperty]
        public ProductDto Product { get; set; }


        [BindProperty]
        public IFormFile Image { get; set; }


        public string SuccessMessage { get; set; }


        private readonly IContosoAPI _contosoApi;


        public ProductCreateModel(IContosoAPI contosoApi)
        {
            _contosoApi = contosoApi;
        }


        public void OnGet()
        {
            IsAdmin = true;
        }


        public async Task<IActionResult> OnPostCreateProductAsync()
        {
            if (Image != null && Image.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    Image.CopyTo(memoryStream);
                    Product.Image = memoryStream.ToArray();
                }

                // --------- 1 - upload to file system -------------
                // var fileName = Path.GetFileName(Image.FileName);
                // var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                // using (var stream = new FileStream(filePath, FileMode.Create))
                // {
                //     Image.CopyTo(stream);
                // }

                // --------- 2 - UPLOAD TO Azure BLOB CONTAINER -------------
                // Upload using container SAS URL
                var containerSasUrl = "https://webshopstoragetest.blob.core.windows.net/storagecontainer?sp=racwdli&st=2025-11-19T03:15:26Z&se=2025-12-10T11:30:26Z&spr=https&sv=2024-11-04&sr=c&sig=BnedqjIBKqmmvh33wtXoLKUyrvRLxOD40du%2ByWTWRX4%3D";
                var blobUrl = await UploadImageToBlobSasAsync(Image, containerSasUrl);
                Product.ImageUrl = blobUrl + "?sp=racwdli&st=2025-11-19T03:15:26Z&se=2025-12-10T11:30:26Z&spr=https&sv=2024-11-04&sr=c&sig=BnedqjIBKqmmvh33wtXoLKUyrvRLxOD40du%2ByWTWRX4%3D";
            }

            var response = await _contosoApi.CreateProductAsync(Product);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToPage("/ProductCreate/ProductCreate");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "An error occurred while creating the product.");
                return Page();
            }
        }

    private async Task<string?> UploadImageToBlobSasAsync(IFormFile image, string containerSasUrl)
    {
        if (image == null || image.Length == 0) return null;

        var sasUri = new Uri(containerSasUrl);
        var containerBase = sasUri.GetLeftPart(UriPartial.Path); // https://account.blob.core.windows.net/container
        var sasQuery = sasUri.Query; // includes leading '?'

        var blobName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
        var encodedBlobName = Uri.EscapeDataString(blobName);
        var uploadUri = new Uri(containerBase + "/" + encodedBlobName + sasQuery);

        using (var client = new HttpClient())
        using (var stream = image.OpenReadStream())
        using (var content = new StreamContent(stream))
        {
            content.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType ?? "application/octet-stream");
            var request = new HttpRequestMessage(HttpMethod.Put, uploadUri)
            {
                Content = content
            };
            request.Headers.Add("x-ms-blob-type", "BlockBlob");

            var resp = await client.SendAsync(request);
            resp.EnsureSuccessStatusCode();
        }

        return containerBase + "/" + encodedBlobName;
    }
}
