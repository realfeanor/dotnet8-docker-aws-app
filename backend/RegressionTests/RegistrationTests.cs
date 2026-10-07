using System.IdentityModel.Tokens.Jwt;
using Business.Concrete;
using Core.Entities.Concrete;
using Core.Utilities.Security.Jwt;
using DataAccess.Concrete.EntityFramework;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Dtos;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace RegressionTests;

[Trait("Category", "Integration")]
public class RegistrationTests
{
    [Fact]
    public void Register_PersistsOnlyReadPermissionsAndIncludesThemInReturnedToken()
    {
        using var connection = OpenDatabase();
        using var context = CreateContext(connection);
        context.Database.EnsureCreated();
        var controller = new WebAPI.Controllers.AuthController(CreateManager(context));

        var response = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(controller.Register(Request("new@example.com")));

        var user = Assert.Single(context.Users);
        Assert.Equal(new[] { "Category.Get", "Product.Get" },
            new EfUserDal(context).GetClaims(user).Select(c => c.Name).OrderBy(n => n));
        Assert.Equal(2, context.UserOperationClaims.Count());
        var token = Assert.IsType<AccessToken>(response.Value);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);
        Assert.Equal(new[] { "Category.Get", "Product.Get" },
            jwt.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).OrderBy(n => n));
    }

    [Fact]
    public void Register_SecondUser_ReusesClaimsAndDuplicateEmailAddsNothing()
    {
        using var connection = OpenDatabase();
        using var context = CreateContext(connection);
        context.Database.EnsureCreated();
        var manager = CreateManager(context);
        Assert.True(manager.Register(Request("first@example.com"), "password").Success);

        Assert.True(manager.Register(Request("second@example.com"), "password").Success);
        Assert.False(manager.Register(Request("first@example.com"), "password").Success);

        Assert.Equal(2, context.Users.Count());
        Assert.Equal(2, context.OperationClaims.Count());
        Assert.Equal(4, context.UserOperationClaims.Count());
    }

    [Fact]
    public void Register_WhenClaimAssignmentFails_RollsBackUserAndClaims()
    {
        using var connection = OpenDatabase();
        using var context = CreateContext(connection, new FailClaimAssignment());
        context.Database.EnsureCreated();
        var manager = CreateManager(context);

        Assert.Throws<InvalidOperationException>(() => manager.Register(Request("new@example.com"), "password"));

        Assert.Empty(context.Users);
        Assert.Empty(context.OperationClaims);
        Assert.Empty(context.UserOperationClaims);
    }

    private static UserForRegisterDto Request(string email) => new()
    {
        Email = email,
        Password = "password",
        FirstName = "Demo",
        LastName = "User"
    };

    private static AuthManager CreateManager(NorthwindContext context)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["TokenOptions:Issuer"] = "tests",
            ["TokenOptions:Audience"] = "tests",
            ["TokenOptions:AccessTokenExpiration"] = "5",
            ["TokenOptions:SecurityKey"] = new string('x', 64)
        }).Build();
        return new AuthManager(new UserManager(new EfUserDal(context)), new JwtHelper(configuration));
    }

    private static SqliteConnection OpenDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static NorthwindContext CreateContext(SqliteConnection connection, params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<NorthwindContext>().UseSqlite(connection).AddInterceptors(interceptors).Options);

    private sealed class FailClaimAssignment : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context.ChangeTracker.Entries<UserOperationClaim>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("Simulated claim assignment failure");
            return result;
        }
    }
}
