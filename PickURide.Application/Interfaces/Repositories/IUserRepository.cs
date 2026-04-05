using PickURide.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<UsersModel?> GetByEmailAsync(string email);
        Task CreateAsync(UsersModel user);
        Task UpdateToken(Guid Id, string Token);
        Task Verified(string email, bool status);
        Task<List<Users.GetUserDto>> GetAllUsersAsync();
        Task<Users.GetUserDto?> GetUserByIdAsync(Guid userId);
        Task DeleteUnverifiedByEmailAsync(string Email);
        Task UpdateAsync(UpdateUserModel user);
        Task<UpdateUserModel> SingleUser(Guid userId);
        Task<bool> ResetPasswordAsync(Guid Id, string newPassword);
        Task<bool> DeleteUserAsync(Guid userId);
    }
}
