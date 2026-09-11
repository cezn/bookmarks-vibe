using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Auth.NpgsqlStores;

public partial class RoleStore : IQueryableRoleStore<ApplicationRole>
{
    public IQueryable<ApplicationRole> Roles => GetAllRoles().ToList().AsQueryable();
}
