using Business.Concrete;
using Core.Entities.Concrete;
using Core.Utilities.Security.Hashing;
using Entities.Dtos;

namespace RegressionTests;

public class AuthManagerTests
{
    [Fact]
    public void Login_WithWrongPassword_RejectsLoginAndPreservesPassword()
    {
        // Arrange: a fake replaces persistence so this test needs no SQL Server.
        var users = CreateUsers();
        var manager = new AuthManager(users, null);

        // Act: call the manager directly to test its business logic.
        var result = manager.Login(new UserForLoginDto { Email = users.User.Email, Password = "wrong" });

        // Assert: failed authentication must not change stored credentials.
        Assert.False(result.Success);
        Assert.True(HashingHelper.VerifyPasswordHash("original-password", users.User.PasswordHash, users.User.PasswordSalt));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Login_WithCorrectPassword_RequiresActiveAccount(bool active, bool expectedSuccess)
    {
        var users = CreateUsers();
        users.User.Status = active;
        var manager = new AuthManager(users, null);

        var result = manager.Login(new UserForLoginDto { Email = users.User.Email, Password = "original-password" });

        Assert.Equal(expectedSuccess, result.Success);
    }

    [Fact]
    public void Login_WithUnknownEmail_ReturnsFailure()
    {
        var manager = new AuthManager(CreateUsers(), null);

        var result = manager.Login(new UserForLoginDto { Email = "missing@example.com", Password = "password" });

        Assert.False(result.Success);
    }

    internal static FakeUsers CreateUsers()
    {
        HashingHelper.CreatePasswordHash("original-password", out var hash, out var salt);
        return new FakeUsers(new User { Email = "user@example.com", PasswordHash = hash, PasswordSalt = salt, Status = true });
    }
}
