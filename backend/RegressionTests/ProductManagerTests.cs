using Business.Concrete;
using Business.Constants;
using Entities.Concrete;

namespace RegressionTests;

public class ProductManagerTests
{
    private readonly FakeProducts _products = new();
    private readonly FakeCategories _categories = new();
    private readonly ProductManager _manager;

    public ProductManagerTests()
    {
        // xUnit creates a fresh instance for every test, so state is never shared.
        _categories.Items.Add(new Category { Id = 2, CategoryName = "Drinks" });
        _manager = new ProductManager(_products, _categories);
    }

    [Fact]
    public void Add_WithValidProduct_PersistsProduct()
    {
        var product = CreateProduct();

        var result = _manager.Add(product);

        Assert.True(result.Success);
        Assert.Same(product, Assert.Single(_products.Items));
    }

    [Fact]
    public void Add_WithDuplicateName_DoesNotPersistProduct()
    {
        _products.Items.Add(CreateProduct(7));

        var result = _manager.Add(CreateProduct());

        Assert.False(result.Success);
        Assert.Equal(Messages.ProductNameAlreadyExists, result.Message);
        Assert.Single(_products.Items);
    }

    [Fact]
    public void Add_WithMissingCategory_DoesNotPersistProduct()
    {
        var product = CreateProduct();
        product.CategoryId = 999;

        var result = _manager.Add(product);

        Assert.False(result.Success);
        Assert.Empty(_products.Items);
    }

    [Fact]
    public void Update_WithValidChanges_PreservesExistingIdentity()
    {
        var existing = CreateProduct(7);
        _products.Items.Add(existing);
        var request = CreateProduct(7);
        request.ProductName = "Apple Soda";
        request.QuantityPerUnit = "2 bottles";
        request.UnitPrice = 20;
        request.UnitsInStock = 10;

        var result = _manager.Update(request);

        Assert.True(result.Success);
        Assert.Same(existing, _products.LastUpdated);
        Assert.Equal(7, existing.Id);
        Assert.Equal("Apple Soda", existing.ProductName);
        Assert.Equal("2 bottles", existing.QuantityPerUnit);
        Assert.Equal(20m, existing.UnitPrice);
        Assert.Equal((short)10, existing.UnitsInStock);
        Assert.Single(_products.Items);
    }

    [Fact]
    public void Update_WithUnchangedName_DoesNotTreatItAsDuplicate()
    {
        _products.Items.Add(CreateProduct(7));

        var result = _manager.Update(CreateProduct(7));

        Assert.True(result.Success);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Update_WithInvalidReferenceOrDuplicateName_DoesNotModifyExistingProduct(bool missingCategory)
    {
        var existing = CreateProduct(7);
        _products.Items.Add(existing);
        var request = CreateProduct(7);
        request.ProductName = "Taken";
        if (missingCategory) request.CategoryId = 999;
        else _products.Items.Add(new Product { Id = 8, ProductName = "Taken", CategoryId = 2 });

        var result = _manager.Update(request);

        Assert.False(result.Success);
        Assert.Equal("Apple Juice", existing.ProductName);
        Assert.Equal(2, existing.CategoryId);
        Assert.Null(_products.LastUpdated);
    }

    [Fact]
    public void Update_WithMissingProduct_ReturnsFailure()
    {
        Assert.False(_manager.Update(CreateProduct(999)).Success);
        Assert.Null(_products.LastUpdated);
    }

    [Fact]
    public void Delete_WithExistingId_RemovesProduct()
    {
        _products.Items.Add(CreateProduct(7));

        var result = _manager.Delete(new Product { Id = 7 });

        Assert.True(result.Success);
        Assert.Empty(_products.Items);
    }

    [Fact]
    public void Delete_WithMissingId_ReturnsFailure()
    {
        Assert.False(_manager.Delete(new Product { Id = 999 }).Success);
    }

    [Fact]
    public void TransactionalOperation_UpdatesWithoutInsertingAnotherProduct()
    {
        _products.Items.Add(CreateProduct(7));

        var result = _manager.TransactionalOperation(CreateProduct(7));

        Assert.True(result.Success);
        Assert.Single(_products.Items);
        Assert.NotNull(_products.LastUpdated);
    }

    internal static Product CreateProduct(int id = 0) => new()
    {
        Id = id, ProductName = "Apple Juice", CategoryId = 2,
        QuantityPerUnit = "1 bottle", UnitPrice = 15, UnitsInStock = 20
    };
}
