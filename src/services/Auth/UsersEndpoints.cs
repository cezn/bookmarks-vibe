using Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auth;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization("AdminOnly").WithTags("Users");

        group.MapGet("", GetAllUsers).WithName("GetAllUsers");
        group.MapGet("{userId}", GetUser).WithName("GetUser");
        group.MapGet("roles", GetAllRoles).WithName("GetAllRoles");
        group.MapGet("{userId}/roles", GetUserRoles).WithName("GetUserRoles");
        group.MapPost("{userId}/roles/{roleId}", AssignRole).WithName("AssignRole");
        group.MapDelete("{userId}/roles/{roleId}", RemoveRole).WithName("RemoveRole");
    }

    /// <summary>
    /// Get all users with their assigned roles
    /// </summary>
    /// <response code="200">List of users with their roles</response>
    /// <response code="500">Internal server error</response>
    [ProducesResponseType(typeof(List<UserRoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private static async Task<IResult> GetAllUsers(UserRoleService userRoleService)
    {
        var users = await userRoleService.GetAllUsersAsync();
        return Results.Ok(users);
    }

    /// <summary>
    /// Get a specific user with their roles
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve</param>
    /// <response code="200">User with their roles</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [ProducesResponseType(typeof(UserRoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private static async Task<IResult> GetUser(string userId, UserRoleService userRoleService)
    {
        var user = await userRoleService.GetUserAsync(userId);
        if (user == null)
            return Results.NotFound(new { error = "User not found" });

        return Results.Ok(user);
    }

    /// <summary>
    /// Get all available roles
    /// </summary>
    /// <response code="200">List of all roles</response>
    /// <response code="500">Internal server error</response>
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private static async Task<IResult> GetAllRoles(UserRoleService userRoleService)
    {
        var roles = await userRoleService.GetAllRolesAsync();
        return Results.Ok(roles);
    }

    /// <summary>
    /// Get roles for a specific user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <response code="200">List of role names assigned to the user</response>
    /// <response code="500">Internal server error</response>
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private static async Task<IResult> GetUserRoles(string userId, UserRoleService userRoleService)
    {
        var roles = await userRoleService.GetUserRolesAsync(userId);
        return Results.Ok(roles);
    }

    /// <summary>
    /// Assign a role to a user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="roleId">The ID of the role to assign</param>
    /// <response code="200">Role assigned successfully</response>
    /// <response code="400">Invalid request or assignment failed</response>
    /// <response code="500">Internal server error</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private static async Task<IResult> AssignRole(string userId, string roleId, UserRoleService userRoleService)
    {
        var result = await userRoleService.AssignRoleToUserAsync(userId, roleId);
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Results.Ok(new { message = "Role assigned successfully" });
    }

    /// <summary>
    /// Remove a role from a user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="roleId">The ID of the role to remove</param>
    /// <response code="200">Role removed successfully</response>
    /// <response code="400">Invalid request or removal failed</response>
    /// <response code="500">Internal server error</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private static async Task<IResult> RemoveRole(string userId, string roleId, UserRoleService userRoleService)
    {
        var result = await userRoleService.RemoveRoleFromUserAsync(userId, roleId);
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Results.Ok(new { message = "Role removed successfully" });
    }
}
