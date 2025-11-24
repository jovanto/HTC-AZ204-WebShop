namespace Contoso.Api.Data;

public class OrderDto
{
    public int Id { get; set; }
    
    public int UserId { get; set; }

    public decimal Total { get; set; }

    public string? Status { get; set; }

    public virtual List<OrderItemDto>? Items { get; set; }
}