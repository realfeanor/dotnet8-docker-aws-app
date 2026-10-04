using System.Linq.Expressions;
using System.Text.Json;
using API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Business.Abstract;
using Business.Concrete;
using Core.Entities.Concrete;
using Core.Utilities.Security.Hashing;
using Core.CrossCuttingConcerns.Caching.Microsoft;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Entities.Dtos;
using Business.BusinessAspects.Autofac;
using Core.Utilities.IoC;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebAPI.Controllers;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS: " + message);
}

HashingHelper.CreatePasswordHash("original-password", out var hash, out var salt);
var users = new FakeUsers(new User { Email = "user@example.com", PasswordHash = hash, PasswordSalt = salt, Status = true });
var auth = new AuthManager(users, null);
Check(!auth.Login(new UserForLoginDto { Email = users.User.Email, Password = "wrong" }).Success,
    "Wrong password is rejected");
Check(HashingHelper.VerifyPasswordHash("original-password", users.User.PasswordHash, users.User.PasswordSalt),
    "Failed login preserves the original password");
users.User.Status = false;
Check(!auth.Login(new UserForLoginDto { Email = users.User.Email, Password = "original-password" }).Success,
    "Disabled account cannot log in");
Check(typeof(AuthController).GetMethod("UpdatePassword") == null && typeof(IAuthService).GetMethod("UpdatePassword") == null,
    "Unauthenticated password replacement is removed");
foreach (var method in new[] { "Add", "Update", "Delete", "TransactionalOperation" })
{
    var security = typeof(ProductManager).GetMethod(method).GetCustomAttributesData()
        .SingleOrDefault(a => a.AttributeType == typeof(SecuredOperation));
    Check(security != null && security.NamedArguments.Any(a => a.MemberName == "Priority" && (int)a.TypedValue.Value == -100),
        method + " uses business-layer authorization before other aspects");
}
Check(typeof(ProductsController).GetMethods().All(m => !m.GetCustomAttributesData().Any(a => a.AttributeType.Name == "AuthorizeAttribute")),
    "Product controller delegates authorization to the business layer");
var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
ServiceTool.Create(new ServiceCollection().AddSingleton<IHttpContextAccessor>(accessor));
var securityAspect = new TestSecuredOperation("Product.Add,Admin");
bool AccessAllowed()
{
    try { securityAspect.CheckAccess(); return true; }
    catch (UnauthorizedAccessException) { return false; }
}
Check(!AccessAllowed(), "SecuredOperation rejects anonymous requests");
accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Other") }, "test"));
Check(!AccessAllowed(), "SecuredOperation rejects users without an allowed role");
accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "test"));
Check(AccessAllowed(), "SecuredOperation allows Admin");
accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Product.Add") }, "test"));
Check(AccessAllowed(), "SecuredOperation preserves the existing Product.Add permission");

using var context = new NorthwindContext(new DbContextOptionsBuilder<NorthwindContext>()
    .UseSqlServer("Server=localhost;Database=Regression;Integrated Security=True;TrustServerCertificate=True").Options);
Check(context.Model.FindEntityType(typeof(Product)).FindProperty("Id").GetColumnName() == "Id",
    "Product primary-key column is named Id");
Check(context.Model.FindEntityType(typeof(Category)).FindProperty("Id").GetColumnName() == "Id",
    "Category primary-key column is named Id");
var categories = new FakeCategories();
var products = new FakeProducts();
var createRequest = JsonSerializer.Deserialize<CategoryForCreateDto>(
    "{\"id\":42,\"categoryName\":\"Example\"}",
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
var createResult = new CategoryController(new CategoryManager(categories, products)).AddCategory(createRequest);
Check(createResult is OkObjectResult && categories.Items.Single().Id == 0,
    "Category creation ignores client IDs and leaves the key for database generation");
Check(context.Model.FindEntityType(typeof(Category)).FindProperty("Id").ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd,
    "Category ID is generated when inserted");
Check(categories.Items.Count == 1, "Adding a category persists it through the repository");
var productRequest = JsonSerializer.Deserialize<ProductForCreateDto>(
    "{\"id\":42,\"productName\":\"Apple Juice\",\"categoryId\":1,\"quantityPerUnit\":\"1 bottle\",\"unitPrice\":15,\"unitsInStock\":20}",
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
categories.Items.Single().Id = 1;
var productResult = new ProductsController(new ProductManager(products, categories)).Add(productRequest);
Check(productResult is OkObjectResult && products.Items.Single().Id == 0,
    "Product creation ignores client IDs and leaves the key for database generation");
var savedProduct = products.Items.Single();
Check(savedProduct.ProductName == "Apple Juice" && savedProduct.CategoryId == 1 &&
    savedProduct.QuantityPerUnit == "1 bottle" && savedProduct.UnitPrice == 15 && savedProduct.UnitsInStock == 20,
    "Product creation preserves the supplied business fields");
Check(context.Model.FindEntityType(typeof(Product)).FindProperty("Id").ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd,
    "Product ID is generated when inserted");
savedProduct.Id = 7;
savedProduct.Category = categories.Items.Single();
savedProduct.Category.Products.Add(savedProduct);
var responseController = new ProductsController(new ProductManager(products, categories));
var readManager = new ProductManager(products, categories);
Check(readManager.GetById(7).Data is Product && readManager.GetList().Data is List<Product> &&
    readManager.GetListByCategory(1).Data is List<Product>,
    "Product manager read methods return entities rather than response DTOs");
var productResponse = ((OkObjectResult)responseController.GetById(7)).Value as ProductResponseDto;
Check(productResponse != null && productResponse.CategoryId == 1 && productResponse.Category.Id == 1 && productResponse.Category.CategoryName == "Example",
    "Controller maps the loaded category to a category response summary");
var responseJson = JsonSerializer.Serialize(productResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
Check(responseJson.Contains("\"categoryId\"") && responseJson.Contains("\"category\"") && !responseJson.Contains("\"products\""),
    "Product response serializes a bidirectional entity graph without cycles");
Check(((OkObjectResult)responseController.GetList()).Value is List<ProductResponseDto> &&
    ((OkObjectResult)responseController.GetListByCategory(1)).Value is List<ProductResponseDto>,
    "Product list endpoints return response DTOs");
var categoryResponses = ((OkObjectResult)new CategoryController(new CategoryManager(categories, products)).GetAllCategories()).Value
    as Core.Utilities.Results.IDataResult<List<CategoryResponseDto>>;
Check(categoryResponses != null && categoryResponses.Data.Single().CategoryName == "Example",
    "Category endpoint preserves the result envelope and returns response DTOs");
Check(!JsonSerializer.Serialize(categoryResponses).Contains("Products"),
    "Category responses exclude entity navigation collections");
Check(responseController.GetById(999) is NotFoundObjectResult,
    "Product detail returns 404 for a missing product");
var productController = new ProductsController(new ProductManager(products, categories));
var updateRequest = new ProductForUpdateDto
{
    Id = 7, ProductName = "Apple Soda", CategoryId = 1,
    QuantityPerUnit = "2 bottles", UnitPrice = 20, UnitsInStock = 10
};
Check(productController.Update(updateRequest) is OkObjectResult &&
    ReferenceEquals(products.LastUpdated, savedProduct) && savedProduct.Id == 7 &&
    savedProduct.ProductName == "Apple Soda" && savedProduct.QuantityPerUnit == "2 bottles" &&
    savedProduct.UnitPrice == 20 && savedProduct.UnitsInStock == 10,
    "Update modifies the existing product and preserves its identity");
var originalProductCount = products.Items.Count;
Check(new ProductManager(products, categories).TransactionalOperation(
    new Product { Id = 7, ProductName = "Apple Soda", CategoryId = 1, QuantityPerUnit = "2 bottles", UnitPrice = 20, UnitsInStock = 10 }).Success &&
    products.Items.Count == originalProductCount,
    "Transaction updates the existing product without inserting its identity again");
updateRequest.CategoryId = 999;
Check(productController.Update(updateRequest) is BadRequestObjectResult && savedProduct.CategoryId == 1,
    "Update rejects a nonexistent category without modifying the product");
updateRequest.CategoryId = 1;
products.Items.Add(new Product { Id = 8, ProductName = "Already Taken", CategoryId = 1 });
updateRequest.ProductName = "Already Taken";
Check(productController.Update(updateRequest) is BadRequestObjectResult && savedProduct.ProductName == "Apple Soda",
    "Update rejects another product's name without modifying the product");
updateRequest.Id = 999;
Check(productController.Update(updateRequest) is BadRequestObjectResult,
    "Updating a nonexistent product returns a business error");
var deleteId = 7;
Check(productController.Delete(deleteId) is OkObjectResult && !products.Items.Contains(savedProduct),
    "Delete uses only the ID and removes the existing product");
Check(productController.Delete(deleteId) is BadRequestObjectResult,
    "Deleting a nonexistent product returns a business error");
using var memory = new MemoryCache(new MemoryCacheOptions());
var cache = new MemoryCacheManager(memory);
cache.Add("Business.Abstract.IProductService.GetListByCategory(1)", "cached", 10);
cache.Add("unrelated", "retained", 10);
cache.RemoveByPattern("IProductService.Get");
Check(!cache.IsAdd("Business.Abstract.IProductService.GetListByCategory(1)") && cache.IsAdd("unrelated"),
    "Cache invalidation removes product entries and preserves unrelated entries");
foreach (var dtoType in new[] { typeof(CategoryForCreateDto), typeof(CategoryForUpdateDto), typeof(ProductForCreateDto), typeof(ProductForUpdateDto) })
    Check(dtoType.GetProperties().All(p => !p.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.ValidationAttribute), true).Any()),
        dtoType.Name + " has no Data Annotation validation rules");
var productValidator = new Business.ValidationRules.FluentValidation.ProductValidator();
Check(!productValidator.Validate(new Product { ProductName = "", UnitPrice = 0 }).IsValid,
    "FluentValidation rejects empty product names and invalid prices");
Check(productValidator.Validate(new Product { ProductName = "Example", CategoryId = 2, UnitPrice = 1, QuantityPerUnit = "" }).IsValid,
    "Packaging description is not required by the product validator");
var categoryValidator = new Business.ValidationRules.FluentValidation.CategoryValidator();
Check(!categoryValidator.Validate(new Category { CategoryName = "" }).IsValid,
    "FluentValidation rejects empty category names");
var categoryController = new CategoryController(new CategoryManager(categories, products));
var existingCategory = categories.Items.Single();
Check(categoryController.UpdateCategory(new CategoryForUpdateDto { Id = 1, CategoryName = "Updated" }) is OkObjectResult &&
    ReferenceEquals(categories.LastUpdated, existingCategory) && existingCategory.Id == 1 && existingCategory.CategoryName == "Updated",
    "Category update modifies the existing entity and preserves its ID");
Check(categoryController.UpdateCategory(new CategoryForUpdateDto { Id = 999, CategoryName = "Missing" }) is BadRequestObjectResult,
    "Updating a nonexistent category returns a business error");
products.Items.Add(new Product { Id = 9, CategoryId = 1 });
Check(categoryController.DeleteCategory(1) is BadRequestObjectResult && categories.Items.Contains(existingCategory),
    "Category with multiple products cannot be deleted");
products.Items.RemoveAll(p => p.CategoryId == 1);
Check(categoryController.DeleteCategory(1) is OkObjectResult && !categories.Items.Contains(existingCategory),
    "Category with no products is deleted by ID alone");
Check(categoryController.DeleteCategory(1) is BadRequestObjectResult,
    "Deleting a nonexistent category returns a business error");
var updateCategoryValidator = new Business.ValidationRules.FluentValidation.CategoryUpdateValidator();
Check(!updateCategoryValidator.Validate(new Category { Id = 0, CategoryName = "Valid" }).IsValid &&
    !updateCategoryValidator.Validate(new Category { Id = 1, CategoryName = "" }).IsValid,
    "Category update validates both ID and name through FluentValidation");
var deleteCategoryValidator = new Business.ValidationRules.FluentValidation.CategoryDeleteValidator();
Check(deleteCategoryValidator.Validate(new Category { Id = 1 }).IsValid && !deleteCategoryValidator.Validate(new Category { Id = 0 }).IsValid,
    "Category deletion validates only the ID through FluentValidation");
foreach (var method in new[] { "Update", "Delete" })
{
    var attributes = typeof(CategoryManager).GetMethod(method).GetCustomAttributesData();
    Check(attributes.Any(a => a.AttributeType == typeof(SecuredOperation)) &&
        attributes.Any(a => a.AttributeType == typeof(Core.Aspects.Autofac.Validation.ValidationAspect)),
        "Category " + method + " uses authorization and validation aspects");
}
var categoryForeignKey = context.Model.FindEntityType(typeof(Product)).GetForeignKeys().Single();
Check(categoryForeignKey.PrincipalEntityType.ClrType == typeof(Category) &&
    categoryForeignKey.Properties.Single().Name == "CategoryId" && categoryForeignKey.IsRequired && !categoryForeignKey.IsUnique,
    "Products have a required many-to-one category relationship");
Check(categoryForeignKey.DeleteBehavior == DeleteBehavior.Restrict,
    "The category relationship restricts deletion of referenced categories");
var migrationScript = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>(context)
    .GenerateScript("20260117130843_First");
Check(migrationScript.Contains("FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([CategoryId])") &&
    migrationScript.Contains("CREATE INDEX [IX_Products_CategoryId]") && !migrationScript.Contains("ON DELETE CASCADE"),
    "Generated migration SQL adds the category foreign key and index without cascading deletion");
Check(!migrationScript.Contains("DROP TABLE") && !migrationScript.Contains("DROP COLUMN"),
    "Relationship migration preserves the existing tables and columns");
Check(categoryForeignKey.DependentToPrincipal.Name == "Category" && categoryForeignKey.PrincipalToDependent.Name == "Products",
    "EF binds both product and category navigation properties to the existing relationship");
var renameScript = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>(context)
    .GenerateScript("20261003132601_AddProductCategoryNavigations");
Check(renameScript.Contains("sp_rename N'[Products].[ProductId]', N'Id', N'COLUMN'") &&
    renameScript.Contains("sp_rename N'[Categories].[CategoryId]', N'Id', N'COLUMN'") &&
    !renameScript.Contains("DROP COLUMN") && !renameScript.Contains("DROP TABLE"),
    "Primary-key migration renames both columns without deleting data");
Check(context.Model.FindEntityType(typeof(Product)).FindProperty("CategoryId").GetColumnName() == "CategoryId" &&
    categoryForeignKey.PrincipalKey.Properties.Single().GetColumnName() == "Id",
    "Products.CategoryId references Categories.Id after the rename");
var missingCategoryResult = new CategoryManager(categories, products).Delete(new Category { Id = 999 });
Check(missingCategoryResult.GetType() == typeof(Core.Utilities.Results.ErrorResult) &&
    missingCategoryResult.Message == Business.Constants.Messages.CategoryNotFound,
    "Missing categories use ErrorResult with the category-not-found message");
var missingProductResult = new ProductManager(products, categories).Update(new Product { Id = 999 });
Check(missingProductResult.GetType() == typeof(Core.Utilities.Results.ErrorResult) &&
    !string.IsNullOrWhiteSpace(missingProductResult.Message),
    "Missing products use ErrorResult with an explanatory message");
foreach (var endpoint in new[] { (typeof(CategoryController), "DeleteCategory"), (typeof(ProductsController), "Delete") })
{
    var method = endpoint.Item1.GetMethod(endpoint.Item2);
    var route = method.GetCustomAttributesData().Single(a => a.AttributeType == typeof(HttpDeleteAttribute));
    Check((string)route.ConstructorArguments.Single().Value == "{id:int}" &&
        method.GetParameters().Single().ParameterType == typeof(int),
        endpoint.Item1.Name + " deletion uses an integer ID in a DELETE route");
}
var loginValidator = new Business.ValidationRules.FluentValidation.UserForLoginValidator();
Check(!loginValidator.Validate(new UserForLoginDto { Email = "user@example.com", Password = null }).IsValid &&
    !loginValidator.Validate(new UserForLoginDto { Email = "invalid", Password = "password" }).IsValid,
    "Login validation rejects missing passwords and malformed email addresses");
var registerValidator = new Business.ValidationRules.FluentValidation.UserForRegisterValidator();
Check(!registerValidator.Validate(new UserForRegisterDto { Email = "user@example.com", Password = "", FirstName = "", LastName = "" }).IsValid,
    "Registration validation rejects empty passwords and names");
Check(!productValidator.Validate(new Product { ProductName = "Example", CategoryId = 2, UnitPrice = 1, UnitsInStock = -1 }).IsValid,
    "Product validation rejects negative stock");
Check(!typeof(ProductManager).GetMethod("TransactionalOperation").GetCustomAttributesData().Any(a =>
    a.AttributeType == typeof(Core.Aspects.Autofac.Transaction.TransactionScopeAspect)),
    "Single-save update does not start an ambient transaction conflicting with SQL retries");
var services = new ServiceCollection();
services.AddSingleton<IHttpContextAccessor>(accessor);
services.AddMemoryCache();
services.AddSingleton<Core.CrossCuttingConcerns.Caching.ICacheManager, MemoryCacheManager>();
services.AddSingleton<System.Diagnostics.Stopwatch>();
ServiceTool.Create(services);
var generator = new Castle.DynamicProxy.ProxyGenerator();
var proxyOptions = new Castle.DynamicProxy.ProxyGenerationOptions
{
    Selector = new Core.Utilities.Interceptors.AspectInterceptorSelector()
};
var proxyCategories = new FakeCategories();
proxyCategories.Items.Add(new Category { Id = 2, CategoryName = "Allowed" });
var proxyProducts = new FakeProducts();
var productProxy = generator.CreateInterfaceProxyWithTarget<IProductService>(
    new ProductManager(proxyProducts, proxyCategories), proxyOptions);
accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Product.Add") }, "test"));
Check(productProxy.Add(new Product { ProductName = "Proxy Product", CategoryId = 2, UnitPrice = 1 }).Success,
    "Real authorization proxy permits Product.Add without Category.Get");
accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Product.Update") }, "test"));
Check(productProxy.Update(new Product { Id = 0, ProductName = "Proxy Product", CategoryId = 2, UnitPrice = 2 }).Success,
    "Real authorization proxy permits Product.Update without Category.Get");
accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
bool denied = false;
try { productProxy.Add(new Product { ProductName = "Denied", CategoryId = 2, UnitPrice = 1 }); }
catch (UnauthorizedAccessException) { denied = true; }
Check(denied && proxyProducts.Items.Count == 1, "Real authorization proxy blocks anonymous writes before persistence");
var authProxy = generator.CreateInterfaceProxyWithTarget<IAuthService>(auth, proxyOptions);
bool invalidLogin = false;
try { authProxy.Login(new UserForLoginDto { Email = "user@example.com", Password = null }); }
catch (FluentValidation.ValidationException) { invalidLogin = true; }
Check(invalidLogin, "Real validation proxy rejects missing login password before hashing");
bool invalidRegistration = false;
try { authProxy.Register(new UserForRegisterDto { Email = "user@example.com", Password = "", FirstName = "Example", LastName = "User" }, ""); }
catch (FluentValidation.ValidationException) { invalidRegistration = true; }
Check(invalidRegistration, "Real validation proxy rejects empty registration passwords before persistence");
bool nullRequest = false;
try { authProxy.Login(null); }
catch (FluentValidation.ValidationException) { nullRequest = true; }
Check(nullRequest, "Real validation proxy rejects null requests with a validation error");
Check(context.Model.FindEntityType(typeof(User)).GetIndexes().Any(i => i.IsUnique && i.Properties.Single().Name == "Email") &&
    context.Model.FindEntityType(typeof(Product)).GetIndexes().Any(i => i.IsUnique && i.Properties.Single().Name == "ProductName"),
    "Database model enforces unique emails and product names");
Console.WriteLine("All backend regression checks passed.");

class FakeUsers(User user) : IUserService
{
    public User User { get; } = user;
    public User GetByMail(string email) => User.Email == email ? User : null;
    public List<OperationClaim> GetClaims(User user) => new();
    public void Add(User user) => throw new NotSupportedException();
    public void Update(User user) => throw new Exception("Login must not update a user");
}
class FakeCategories : ICategoryDal
{
    public bool Exists(int categoryId) => Items.Any(c => c.Id == categoryId);
    public Category LastUpdated { get; private set; }
    public List<Category> Items { get; } = new();
    public void Add(Category category) => Items.Add(category);
    public Category Get(Expression<Func<Category, bool>> filter) => Items.AsQueryable().SingleOrDefault(filter);
    public IList<Category> GetList(Expression<Func<Category, bool>> filter = null) =>
        filter == null ? Items.ToList() : Items.AsQueryable().Where(filter).ToList();
    public void Delete(Category category) => Items.Remove(category);
    public void Update(Category category) => LastUpdated = category;
}

class FakeProducts : IProductDal
{
    public Product GetWithCategory(int productId) => Items.SingleOrDefault(p => p.Id == productId);
    public List<Product> GetListWithCategory(int? categoryId = null) => Items
        .Where(p => !categoryId.HasValue || p.CategoryId == categoryId.Value).ToList();
    public bool ProductNameExists(string name, int? excludedProductId = null) => Items.Any(p => p.ProductName == name && p.Id != excludedProductId);
    public bool HasProductsInCategory(int categoryId) => Items.Any(p => p.CategoryId == categoryId);
    public Product LastUpdated { get; private set; }
    public List<Product> Items { get; } = new();
    public void Add(Product product) => Items.Add(product);
    public Product Get(Expression<Func<Product, bool>> filter) => Items.AsQueryable().SingleOrDefault(filter);
    public IList<Product> GetList(Expression<Func<Product, bool>> filter = null) =>
        filter == null ? Items.ToList() : Items.AsQueryable().Where(filter).ToList();
    public void Delete(Product product) => Items.Remove(product);
    public void Update(Product product) => LastUpdated = product;
}

class TestSecuredOperation(string roles) : SecuredOperation(roles)
{
    public void CheckAccess() => OnBefore(null);
}
