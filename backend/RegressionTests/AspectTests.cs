using System.Security.Claims;
using Business.Abstract;
using Business.Concrete;
using Castle.DynamicProxy;
using Core.CrossCuttingConcerns.Caching;
using Core.CrossCuttingConcerns.Caching.Microsoft;
using Core.CrossCuttingConcerns.Logging.Log4Net;
using Core.Utilities.Interceptors;
using Core.Utilities.IoC;
using Entities.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace RegressionTests;

// Only aspect tests use the application's static ServiceTool and log4net repository.
// Keep those tests in one non-parallel collection; ordinary unit tests remain independent.
[CollectionDefinition("Aspects", DisableParallelization = true)]
public class AspectCollection : ICollectionFixture<AspectFixture> { }

public sealed class AspectFixture : IDisposable
{
    public HttpContextAccessor Accessor { get; } = new();
    private readonly ServiceProvider _services;

    public AspectFixture()
    {
        _services = new ServiceCollection()
            .AddSingleton<IHttpContextAccessor>(Accessor)
            .AddMemoryCache()
            .AddSingleton<ICacheManager, MemoryCacheManager>()
            .BuildServiceProvider();
        ServiceTool.Initialize(_services);
        var path = Path.Combine(Path.GetTempPath(), $"backend-tests-{Guid.NewGuid()}.config");
        try
        {
            // No appenders: proxy exception logging must not write files or connect to SQL.
            File.WriteAllText(path, "<log4net />");
            LoggerServiceBase.Configure("unused", path);
        }
        finally { File.Delete(path); }
    }

    public T Proxy<T>(T target) where T : class => new ProxyGenerator().CreateInterfaceProxyWithTarget<T>(
        target, new ProxyGenerationOptions { Selector = new AspectInterceptorSelector() });

    public void Dispose() => _services.Dispose();
}

[Collection("Aspects")]
[Trait("Category", "Integration")]
public class AspectTests
{
    private readonly AspectFixture _fixture;
    public AspectTests(AspectFixture fixture)
    {
        _fixture = fixture;
        _fixture.Accessor.HttpContext = new DefaultHttpContext();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Other")]
    public void Add_WithoutAllowedRole_ThrowsBeforePersistence(string role)
    {
        SetRole(role);
        var products = new FakeProducts();
        var proxy = CreateProductProxy(products);

        Assert.Throws<UnauthorizedAccessException>(() => proxy.Add(ProductManagerTests.CreateProduct()));
        Assert.Empty(products.Items);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Product.Add")]
    public void Add_WithAllowedRole_DoesNotRequireCategoryGet(string role)
    {
        SetRole(role);
        var products = new FakeProducts();
        var proxy = CreateProductProxy(products);

        var result = proxy.Add(ProductManagerTests.CreateProduct());

        Assert.True(result.Success);
        Assert.Single(products.Items);
    }

    [Fact]
    public void Update_WithProductUpdateRole_DoesNotRequireCategoryGet()
    {
        SetRole("Product.Update");
        var products = new FakeProducts();
        products.Items.Add(ProductManagerTests.CreateProduct(7));
        var proxy = CreateProductProxy(products);

        Assert.True(proxy.Update(ProductManagerTests.CreateProduct(7)).Success);
        Assert.NotNull(products.LastUpdated);
    }

    [Fact]
    public void Add_AnonymousWithInvalidProduct_AuthorizationRunsBeforeValidation()
    {
        var products = new FakeProducts();
        var proxy = CreateProductProxy(products);

        Assert.Throws<UnauthorizedAccessException>(() => proxy.Add(new Entities.Concrete.Product()));
        Assert.Empty(products.Items);
    }

    [Fact]
    public void Add_WithAllowedRoleAndInvalidProduct_ValidationBlocksPersistence()
    {
        SetRole("Product.Add");
        var products = new FakeProducts();
        var proxy = CreateProductProxy(products);

        Assert.Throws<ValidationException>(() => proxy.Add(new Entities.Concrete.Product()));
        Assert.Empty(products.Items);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Login_WithMissingPassword_ValidationRunsBeforeHashing(string password)
    {
        var proxy = _fixture.Proxy<IAuthService>(new AuthManager(AuthManagerTests.CreateUsers(), null));

        Assert.Throws<ValidationException>(() => proxy.Login(new UserForLoginDto { Email = "user@example.com", Password = password }));
    }

    [Fact]
    public void Login_WithNullRequest_ThrowsValidationException()
    {
        var proxy = _fixture.Proxy<IAuthService>(new AuthManager(AuthManagerTests.CreateUsers(), null));

        Assert.Throws<ValidationException>(() => proxy.Login(null));
    }

    [Fact]
    public void Register_WithEmptyPassword_ValidationBlocksPersistence()
    {
        var proxy = _fixture.Proxy<IAuthService>(new AuthManager(AuthManagerTests.CreateUsers(), null));

        Assert.Throws<ValidationException>(() => proxy.Register(new UserForRegisterDto
        {
            Email = "new@example.com", Password = "", FirstName = "Example", LastName = "User"
        }, ""));
    }

    [Fact]
    public void Update_InvalidatesPreviouslyCachedProductReads()
    {
        SetRole("Admin");
        var products = new FakeProducts();
        products.Items.Add(ProductManagerTests.CreateProduct(7));
        var proxy = CreateProductProxy(products);
        var cached = proxy.GetListByCategory(2);
        Assert.Same(cached, proxy.GetListByCategory(2));
        var request = ProductManagerTests.CreateProduct(7);
        request.ProductName = "Updated";

        Assert.True(proxy.Update(request).Success);

        var refreshed = proxy.GetListByCategory(2);
        Assert.NotSame(cached, refreshed);
        Assert.Equal("Updated", Assert.Single(refreshed.Data).ProductName);
    }

    private IProductService CreateProductProxy(FakeProducts products)
    {
        // Clear singleton cache between tests while keeping it shared by real aspects.
        ServiceTool.ServiceProvider.GetRequiredService<ICacheManager>().RemoveByPattern("IProductService.Get");
        var categories = new FakeCategories();
        categories.Items.Add(new Entities.Concrete.Category { Id = 2, CategoryName = "Drinks" });
        return _fixture.Proxy<IProductService>(new ProductManager(products, categories));
    }

    private void SetRole(string role) => _fixture.Accessor.HttpContext.User = role == null
        ? new ClaimsPrincipal(new ClaimsIdentity())
        : new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "test"));
}
