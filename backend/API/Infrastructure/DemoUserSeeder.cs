using Core.Entities.Concrete;
using Core.Utilities.Security.Hashing;
using DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;

namespace API.Infrastructure;

public sealed class DemoUserOptions
{
    public const string SectionName = "DemoUsers";

    public bool Enabled { get; set; }
    public DemoAccountOptions Admin { get; set; } = new();
    public DemoAccountOptions User { get; set; } = new();
}

public sealed class DemoAccountOptions
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public static class DemoUserSeeder
{
    private static readonly string[] AdminClaims = ["Admin"];
    private static readonly string[] UserClaims = ["Product.Get", "Category.Get"];

    public static void Seed(NorthwindContext context, DemoUserOptions options)
    {
        if (!options.Enabled)
        {
            return;
        }

        Validate(options);

        var strategy = context.Database.CreateExecutionStrategy();
        strategy.Execute(() =>
        {
            context.ChangeTracker.Clear();
            using var transaction = context.Database.BeginTransaction();

            var claimsByName = EnsureClaims(context, AdminClaims.Concat(UserClaims));
            var admin = EnsureUser(context, options.Admin);
            var user = EnsureUser(context, options.User);
            context.SaveChanges();

            ReplaceClaims(context, admin.Id, AdminClaims.Select(name => claimsByName[name].Id));
            ReplaceClaims(context, user.Id, UserClaims.Select(name => claimsByName[name].Id));
            context.SaveChanges();

            transaction.Commit();
        });
    }

    private static Dictionary<string, OperationClaim> EnsureClaims(
        NorthwindContext context,
        IEnumerable<string> claimNames)
    {
        var result = new Dictionary<string, OperationClaim>(StringComparer.Ordinal);

        foreach (var claimName in claimNames.Distinct(StringComparer.Ordinal))
        {
            var claim = context.OperationClaims.FirstOrDefault(item => item.Name == claimName);
            if (claim is null)
            {
                claim = new OperationClaim { Name = claimName };
                context.OperationClaims.Add(claim);
            }

            result.Add(claimName, claim);
        }

        return result;
    }

    private static User EnsureUser(NorthwindContext context, DemoAccountOptions account)
    {
        var user = context.Users.FirstOrDefault(item => item.Email == account.Email);
        if (user is null)
        {
            user = new User { Email = account.Email };
            context.Users.Add(user);
        }

        HashingHelper.CreatePasswordHash(account.Password, out var passwordHash, out var passwordSalt);
        user.FirstName = account.FirstName;
        user.LastName = account.LastName;
        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;
        user.Status = true;

        return user;
    }

    private static void ReplaceClaims(
        NorthwindContext context,
        int userId,
        IEnumerable<int> operationClaimIds)
    {
        var existingAssignments = context.UserOperationClaims.Where(item => item.UserId == userId);
        context.UserOperationClaims.RemoveRange(existingAssignments);

        context.UserOperationClaims.AddRange(operationClaimIds.Select(operationClaimId => new UserOperationClaim
        {
            UserId = userId,
            OperationClaimId = operationClaimId
        }));
    }

    private static void Validate(DemoUserOptions options)
    {
        ValidateAccount(options.Admin, "Admin");
        ValidateAccount(options.User, "User");

        if (string.Equals(options.Admin.Email, options.User.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Demo admin and user email addresses must be different.");
        }
    }

    private static void ValidateAccount(DemoAccountOptions account, string accountName)
    {
        if (string.IsNullOrWhiteSpace(account.FirstName) ||
            string.IsNullOrWhiteSpace(account.LastName) ||
            string.IsNullOrWhiteSpace(account.Email) ||
            string.IsNullOrWhiteSpace(account.Password))
        {
            throw new InvalidOperationException($"DemoUsers:{accountName} configuration is incomplete.");
        }
    }
}
