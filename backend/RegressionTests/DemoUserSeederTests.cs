using API.Infrastructure;
using Core.Utilities.Security.Hashing;
using DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace RegressionTests;

[Trait("Category", "Integration")]
public class DemoUserSeederTests
{
    [Fact]
    public void Seed_CreatesExpectedUsersAndIsRepeatable()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = new NorthwindContext(
            new DbContextOptionsBuilder<NorthwindContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        var options = CreateOptions();

        DemoUserSeeder.Seed(context, options);
        DemoUserSeeder.Seed(context, options);

        Assert.Equal(2, context.Users.Count());
        Assert.Equal(3, context.OperationClaims.Count());
        Assert.Equal(3, context.UserOperationClaims.Count());

        var admin = context.Users.Single(item => item.Email == options.Admin.Email);
        var user = context.Users.Single(item => item.Email == options.User.Email);
        Assert.True(HashingHelper.VerifyPasswordHash(
            options.Admin.Password,
            admin.PasswordHash,
            admin.PasswordSalt));
        Assert.True(HashingHelper.VerifyPasswordHash(
            options.User.Password,
            user.PasswordHash,
            user.PasswordSalt));

        Assert.Equal(["Admin"], ClaimsFor(context, admin.Id));
        Assert.Equal(["Category.Get", "Product.Get"], ClaimsFor(context, user.Id));
    }

    [Fact]
    public void Seed_WhenDisabled_DoesNotCreateUsers()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = new NorthwindContext(
            new DbContextOptionsBuilder<NorthwindContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();

        DemoUserSeeder.Seed(context, new DemoUserOptions { Enabled = false });

        Assert.Empty(context.Users);
        Assert.Empty(context.OperationClaims);
        Assert.Empty(context.UserOperationClaims);
    }

    private static string[] ClaimsFor(NorthwindContext context, int userId) =>
        (from assignment in context.UserOperationClaims
         join claim in context.OperationClaims on assignment.OperationClaimId equals claim.Id
         where assignment.UserId == userId
         orderby claim.Name
         select claim.Name).ToArray();

    private static DemoUserOptions CreateOptions() => new()
    {
        Enabled = true,
        Admin = new DemoAccountOptions
        {
            FirstName = "Demo",
            LastName = "Admin",
            Email = "admin@stockroom.local",
            Password = "AdminDemo!2026"
        },
        User = new DemoAccountOptions
        {
            FirstName = "Demo",
            LastName = "User",
            Email = "user@stockroom.local",
            Password = "UserDemo!2026"
        }
    };
}
