using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Contoso.Api.Data;

namespace Contoso.Api.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User{ get; set; }

        public decimal Total { get; set; }

        public OrderStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual IEnumerable<OrderItem>? Items { get; set; }

        public Order()
        {
            Id = Math.Abs(Guid.NewGuid().GetHashCode());
        }
    }
}