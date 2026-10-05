using Business.Concrete;
using Business.Constants;
using Entities.Concrete;

namespace RegressionTests;

public class CategoryManagerTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Delete_WithProducts_ReturnsFailureAndPreservesCategory(int productCount)
    {
        var categories = new FakeCategories();
        categories.Items.Add(new Category { Id = 2, CategoryName = "Drinks" });
        var products = new FakeProducts();
        for (int i = 0; i < productCount; i++) products.Items.Add(new Product { Id = i + 1, CategoryId = 2 });
        var manager = new CategoryManager(categories, products);

        var result = manager.Delete(new Category { Id = 2 });

        Assert.False(result.Success);
        Assert.Equal(Messages.CategoryHasProducts, result.Message);
        Assert.Single(categories.Items);
    }

    [Fact]
    public void Delete_WithoutProducts_RemovesCategory()
    {
        var categories = new FakeCategories();
        categories.Items.Add(new Category { Id = 2, CategoryName = "Drinks" });
        var manager = new CategoryManager(categories, new FakeProducts());

        var result = manager.Delete(new Category { Id = 2 });

        Assert.True(result.Success);
        Assert.Empty(categories.Items);
    }

    [Fact]
    public void Delete_WithMissingCategory_ReturnsCategoryNotFound()
    {
        var manager = new CategoryManager(new FakeCategories(), new FakeProducts());

        var result = manager.Delete(new Category { Id = 999 });

        Assert.False(result.Success);
        Assert.Equal(Messages.CategoryNotFound, result.Message);
    }

    [Fact]
    public void Update_PreservesExistingCategoryIdentity()
    {
        var categories = new FakeCategories();
        var existing = new Category { Id = 2, CategoryName = "Drinks" };
        categories.Items.Add(existing);
        var manager = new CategoryManager(categories, new FakeProducts());

        var result = manager.Update(new Category { Id = 2, CategoryName = "Updated" });

        Assert.True(result.Success);
        Assert.Same(existing, categories.LastUpdated);
        Assert.Equal(2, existing.Id);
        Assert.Equal("Updated", existing.CategoryName);
    }
}
