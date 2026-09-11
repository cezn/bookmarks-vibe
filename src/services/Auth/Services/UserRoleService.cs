using Auth.NpgsqlStores;
using Microsoft.AspNetCore.Identity;

namespace Auth.Services;

public class UserRoleDto
{
    public string Id { get; set; } = default!;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class RoleDto
{
    public string Id { get; set; } = default!;
    public string? Name { get; set; }
}

public class UserRoleService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
{
    public async Task<List<UserRoleDto>> GetAllUsersAsync()
    {
        var users = userManager.Users.ToList();
        var userDtos = new List<UserRoleDto>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            userDtos.Add(
                new UserRoleDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList(),
                }
            );
        }

        return userDtos;
    }

    public async Task<UserRoleDto?> GetUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return null;

        var roles = await userManager.GetRolesAsync(user);
        return new UserRoleDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles.ToList(),
        };
    }

    public async Task<List<RoleDto>> GetAllRolesAsync()
    {
        var roles = roleManager.Roles.ToList();
        return roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name }).ToList();
    }

    public async Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null)
            return IdentityResult.Failed(new IdentityError { Description = "Role not found" });

        var isInRole = await userManager.IsInRoleAsync(user, role.Name!);
        if (isInRole)
            return IdentityResult.Failed(new IdentityError { Description = "User already has this role" });

        return await userManager.AddToRoleAsync(user, role.Name!);
    }

    public async Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null)
            return IdentityResult.Failed(new IdentityError { Description = "Role not found" });

        return await userManager.RemoveFromRoleAsync(user, role.Name!);
    }

    public async Task<List<string>> GetUserRolesAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return new List<string>();

        var roles = await userManager.GetRolesAsync(user);
        return roles.ToList();
    }
}
