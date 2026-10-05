using System.Text.Json;
using API.Controllers;
using Business.Concrete;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Controllers;

namespace RegressionTests;

public class ControllerTests
{
    [Fact]
    public void AddProduct_IgnoresClientIdAndMapsBusinessFields()
    {
        var request = JsonSerializer.Deserialize<ProductForCreateDto>(
            "{\"id\":42,\"productName\":\"Apple Juice\",\"categoryId\":2,\"quantityPerUnit\":\"1 bottle\",\"unitPrice\":15,\"unitsInStock\":20}",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var products = new FakeProducts();
        var categories = new FakeCategories();
        categories.Items.Add(new Category { Id = 2 });
        var controller = new ProductsController(new ProductManager(products, categories));

        var result = controller.Add(request);

        Assert.IsType<OkObjectResult>(result);
        var product = Assert.Single(products.Items);
        Assert.Equal(0, product.Id);
        Assert.Equal("Apple Juice", product.ProductName);
        Assert.Equal(2, product.CategoryId);
        Assert.Equal("1 bottle", product.QuantityPerUnit);
        Assert.Equal(15m, product.UnitPrice);
        Assert.Equal((short)20, product.UnitsInStock);
    }

    [Fact]
    public void AddCategory_IgnoresClientId()
    {
        var request = JsonSerializer.Deserialize<CategoryForCreateDto>("{\"id\":42,\"categoryName\":\"Drinks\"}",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var categories = new FakeCategories();
        var controller = new CategoryController(new CategoryManager(categories, new FakeProducts()));

        Assert.IsType<OkObjectResult>(controller.AddCategory(request));
        Assert.Equal(0, Assert.Single(categories.Items).Id);
    }

    [Fact]
    public void GetProduct_MapsCategoryWithoutSerializingEntityCycles()
    {
        var category = new Category { Id = 2, CategoryName = "Drinks" };
        var product = ProductManagerTests.CreateProduct(7);
        product.Category = category;
        category.Products.Add(product);
        var products = new FakeProducts();
        products.Items.Add(product);
        var controller = new ProductsController(new ProductManager(products, new FakeCategories()));

        var result = Assert.IsType<OkObjectResult>(controller.GetById(7));
        var response = Assert.IsType<ProductResponseDto>(result.Value);

        Assert.Equal(7, response.Id);
        Assert.Equal(2, response.Category.Id);
        Assert.Equal("Drinks", response.Category.CategoryName);
        Assert.DoesNotContain("products", JsonSerializer.Serialize(response).ToLowerInvariant());
    }

    [Fact]
    public void GetProduct_WithMissingId_ReturnsNotFound()
    {
        var controller = new ProductsController(new ProductManager(new FakeProducts(), new FakeCategories()));

        Assert.IsType<NotFoundObjectResult>(controller.GetById(999));
    }
}
