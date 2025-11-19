using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Contoso.WebApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contoso.WebApp.Extensions;
public class ProductModel : PageModel
{
    private readonly IContosoAPI _contosoAPI;

    public ProductDto Product { get; set; }

    public string ErrorMessage { get; set; }

    public string SuccessMessage { get; set; }

    public bool isAdmin { get; set; }


    public ProductModel(IContosoAPI contosoAPI)
    {
        _contosoAPI = contosoAPI;
    }
   
    public async Task OnGetAsync(int id)
    {
        Console.WriteLine("ProductModel.OnGetAsync");
        
        var response = await _contosoAPI.GetProductAsync(id);

        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Failed to retrieve product";
        }

        if (HttpContext.Session.Get<string>("SuccessMessage") != null)
        {
            SuccessMessage = HttpContext.Session.Get<string>("SuccessMessage") ?? string.Empty;
            HttpContext.Session.Remove("SuccessMessage");
        }

        Product = response.Content;

        // get the product.ImageUrl and check blob metadata for 'releasedate'
        // if releasedate is in the future, replace with comingsoon image URL (SAS included)
        var sas = "?sp=racwdli&st=2025-11-19T03:15:26Z&se=2025-12-10T11:30:26Z&spr=https&sv=2024-11-04&sr=c&sig=BnedqjIBKqmmvh33wtXoLKUyrvRLxOD40du%2ByWTWRX4%3D";
        var comingSoonUrl = "https://webshopstoragetest.blob.core.windows.net/storagecontainer/comingsoon.jpeg" + sas;

        if (!string.IsNullOrEmpty(Product?.ImageUrl))
        {
            // ensure we target the blob with SAS so we can read metadata
            var targetUrl = Product.ImageUrl.Contains("?") ? Product.ImageUrl : Product.ImageUrl + sas;

            try
            {
                var blobClient = new BlobClient(new Uri(targetUrl));
                var propsResponse = await blobClient.GetPropertiesAsync();
                var metadata = propsResponse.Value.Metadata;

                if (metadata != null && metadata.TryGetValue("releasedate", out var releasedateValue))
                {
                    if (!string.IsNullOrEmpty(releasedateValue) && DateTime.TryParse(releasedateValue, out var releaseDate))
                    {
                        if (releaseDate.ToUniversalTime() > DateTime.UtcNow)
                        {
                            Product.ImageUrl = comingSoonUrl;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log if needed; do not block page render on metadata failures
                Console.WriteLine($"Failed to read blob metadata for {targetUrl}: {ex.Message}");
            }
        }

        isAdmin = true;
    }

    public async Task<IActionResult> OnPostAddToCart(int id)
    {

        var response = await _contosoAPI.GetProductAsync(id);

        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Failed to retrieve product";
            return Page();
        }

        Product = response.Content;

        List<OrderItemDto> orderItems = HttpContext.Session.Get<List<OrderItemDto>>("OrderItems") ?? new List<OrderItemDto>();

        var existingOrderItem = orderItems.FirstOrDefault(oi => oi.ProductId == id);

        if (existingOrderItem != null)
        {
            existingOrderItem.Quantity++;
        }
        else
        {
            orderItems.Add(new OrderItemDto
            {
                ProductId = id,
                Quantity = 1,
                Price = Product.Price,
                Product = Product
            });
        }

        int cartCount = HttpContext.Session.Get<int>("CartCount");
        
        HttpContext.Session.Set("OrderItems", orderItems);

        HttpContext.Session.Set("SuccessMessage", "Product added to cart");

        HttpContext.Session.Set("CartCount", cartCount + 1);

        return RedirectToPage(new { id });
    }
}