
namespace PickURide.Application.Models
{
    public class AdminModel
    {
        public Guid AdminId { get; set; }

        public string FullName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string? PhoneNumber { get; set; }

        public string PasswordHash { get; set; } = null!;

        public string Role { get; set; } = null!;

        public DateTime? CreatedAt { get; set; }

        public bool? IsActive { get; set; }
    }
}
