using System.Runtime.CompilerServices;
using AutoMapper;
using Contoso.Api.Data;
using Contoso.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Contoso.Api.Services;

public class ProductsService : IProductsService
{
    private readonly ContosoDbContext _context;
    private readonly IMapper _mapper;

    public ProductsService(ContosoDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
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
}