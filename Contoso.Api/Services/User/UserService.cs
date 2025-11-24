using System.Threading.Tasks;
using Contoso.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Contoso.Api.Services;
public class UserService : IUserService
{
    private readonly ContosoDbContext _context;

    public UserService(ContosoDbContext context)
    {
        _context = context;
    }

    public async Task<UserInfoDto> GetUserInfo(int userId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new Exception("User not found");
        }

        return new UserInfoDto
        {
            Name = user.Name,
            Email = user.Email,
            Address = user.Address
        };    
    }

    public async Task UpdateUserInfo(UserInfoDto userInfo, int userId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new Exception("User not found");
        }

        user.Name = userInfo.Name ?? user.Name;
        user.Email = userInfo.Email ?? user.Email;
        user.Address = userInfo.Address ?? user.Address;

        await _context.SaveChangesAsync();
    }
}
