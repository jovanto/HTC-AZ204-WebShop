using System.Runtime.CompilerServices;
using AutoMapper;
using Contoso.Api.Data;
using Contoso.Api.Models;
using Microsoft.EntityFrameworkCore;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.EventGrid;


namespace Contoso.Api.Services;

public class ProductsService : IProductsService
{
    private readonly ContosoDbContext _context;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public ProductsService(ContosoDbContext context, IMapper mapper, IConfiguration configuration)
    {
        _context = context;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(QueryParameters queryParameters)
    {
        var allProducts = await _context.Products
                            .Where(p =>  p.Category == queryParameters.filterText || string.IsNullOrEmpty(queryParameters.filterText))
                            .ToListAsync();

        var totalCount = allProducts.Count;
        
        var products = allProducts
                            .Skip(queryParameters.StartIndex) 
                            .Take(queryParameters.PageSize)
                            .ToList();

        var pagedProducts = new PagedResult<ProductDto>
        {
            Items = _mapper.Map<List<ProductDto>>(products),
            TotalCount = totalCount,
            PageSize = queryParameters.PageSize,
            PageNumber = queryParameters.PageNumber
        };


        return pagedProducts;
    }

    public async Task<ProductDto> GetProductAsync(int id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateProductAsync(ProductDto product)
    {
        var productModel = _mapper.Map<Product>(product);

        _context.Products.Add(productModel);

        await _context.SaveChangesAsync();

        return _mapper.Map<ProductDto>(productModel);
    }

    public async Task<ProductDto> UpdateProductAsync(ProductDto product)
    {
        var existingProduct = await _context.Products.AsNoTracking().FirstAsync(x => x.Id == product.Id);

        if  (existingProduct == null)
        {
            return null;
        }

        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        
        if (existingProduct.ImageUrl != product.ImageUrl)
        {
            existingProduct.ImageUrl = product.ImageUrl;
        }


        _context.Entry(existingProduct).State = EntityState.Modified;

        await _context.SaveChangesAsync();

    // After successful save, publish Product Updated event to Event Grid
    await SendOrderEventAsync();

    return _mapper.Map<ProductDto>(existingProduct);
    }

    public async Task DeleteProductAsync(int id)
    {
        var product = await _context.Products.AsNoTracking().FirstAsync(x => x.Id == id);

        _context.Products.Remove(product);

        await _context.SaveChangesAsync();
    }

    public async Task<List<string>> GetProductCategories()
    {
        return await _context.Products.Select(x => x.Category).Distinct().ToListAsync();
    }

 
    public async Task SendOrderEventAsync()
    {
        var topicEndpoint = _configuration["EventGrid:TopicEndpoint"];

        var _client = new EventGridPublisherClient(
            new Uri(topicEndpoint),
            new AzureKeyCredential("Ah4Rkj51qqGUzgOPOWykNAOD4egi7LlybF639WDKI72PwmssYSHFJQQJ99BKAC24pbEXJ3w3AAABAZEGy6Y0"));
            
        var evt = new EventGridEvent(
            subject: "smartwebshop/orders",
            eventType: "Contoso.SmartWebShop.OrderCreated",
            dataVersion: "1.0",
            data: new
            {
                OrderId = 123,
                Total = 49.99m,
                CreatedAt = DateTime.UtcNow
            });
 
        await _client.SendEventAsync(evt);
    }

}