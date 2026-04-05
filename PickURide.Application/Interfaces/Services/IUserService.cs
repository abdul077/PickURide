using PickURide.Application.Models;

namespace PickURide.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<string> RegisterUserAsync(RegisterRequest request);
        Task<JwtResponse> LoginUserAsync(LoginRequest request);
        Task<string> VerifyUser(string email,bool status);
        Task<List<Users.GetUserDto>> GetAllUsersAsync();
        Task<Users.GetUserDto?> GetUserByIdAsync(Guid userId);
        Task<UsersModel?> GetUserByEmailAsync(string email);
        Task UpdateUserAsync(UpdateUserModel user);
        Task<UpdateUserModel> SingleUser(Guid userId);
        Task<bool> ResetPasswordAsync(string email, string newPassword);
        Task<bool> DeleteUserAsync(Guid userId);
    }
}
