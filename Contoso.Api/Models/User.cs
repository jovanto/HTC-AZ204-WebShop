using System.ComponentModel.DataAnnotations;

namespace Contoso.Api.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }

        public string? PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; }

        public User()
        {
            Id = Math.Abs(Guid.NewGuid().GetHashCode());
        }
    }
}