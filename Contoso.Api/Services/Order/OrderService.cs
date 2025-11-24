using AutoMapper;
using Contoso.Api.Data;
using Contoso.Api.Models;
using Contoso.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Contoso.Api.Services;

public class OrderService : IOrderService
{
    private readonly ContosoDbContext _context;
    private readonly IMapper _mapper;

    public OrderService(ContosoDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(int userId)
    {
         var orders = await _context.Orders
                                    .Where(o => o.UserId == userId)
                                    .Include(o => o.Items!)
                                    .ThenInclude(o => o.Product)
                                    .ToListAsync();

        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }

    public async Task<OrderDto> GetOrderByIdAsync(int id, int userId)
    {
        var order = await _context.Orders
                            .Where(o => o.UserId == userId && o.Id == id)
                            .Include(o => o.Items!)
                            .ThenInclude(o => o.Product)
                            .FirstOrDefaultAsync();

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<OrderDto> CreateOrderAsync(OrderDto orderDto)
    {
        var orderItems = orderDto.Items?.Select(item => new OrderItem
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = item.Price
        }).ToList();

        var newOrder = new Order
        {
            UserId = orderDto.UserId,
            Total = orderDto.Total,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = orderItems
        };

        _context.Orders.Add(newOrder);
        await _context.SaveChangesAsync();

        return _mapper.Map<OrderDto>(newOrder);     
    }
}