using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Auth.NpgsqlStores;

public partial class UserStore(NpgsqlDataSource db)
    : IUserStore<ApplicationUser>,
        IUserClaimStore<ApplicationUser>,
        IUserLoginStore<ApplicationUser>,
        IUserRoleStore<ApplicationUser>,
        IUserPasswordStore<ApplicationUser>,
        IUserSecurityStampStore<ApplicationUser>,
        IUserPasskeyStore<ApplicationUser>,
        IUserEmailStore<ApplicationUser>,
        IUserTwoFactorStore<ApplicationUser>,
        IUserLockoutStore<ApplicationUser>,
        IUserPhoneNumberStore<ApplicationUser>,
        IUserAuthenticatorKeyStore<ApplicationUser>,
        IUserTwoFactorRecoveryCodeStore<ApplicationUser>,
        IQueryableUserStore<ApplicationUser>,
        IUserAuthenticationTokenStore<ApplicationUser> { }
