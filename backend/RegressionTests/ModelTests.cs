using Core.Entities.Concrete;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RegressionTests;

[Trait("Category", "Model")]
public class ModelTests
{
    [Theory]
    [InlineData(typeof(Product))]
    [InlineData(typeof(Category))]
    public void PrimaryKey_IsNamedIdAndGeneratedOnInsert(Type type)
    {
        using var context = CreateContext();

        var property = context.Model.FindEntityType(type).FindProperty("Id");

        Assert.Equal("Id", property.GetColumnName());
        Assert.Equal(ValueGenerated.OnAdd, property.ValueGenerated);
    }

    [Fact]
    public void ProductCategory_IsRequiredManyToOneWithRestrictedDeletion()
    {
        using var context = CreateContext();

        var relationship = Assert.Single(context.Model.FindEntityType(typeof(Product)).GetForeignKeys());

        Assert.Equal(typeof(Category), relationship.PrincipalEntityType.ClrType);
        Assert.Equal("CategoryId", Assert.Single(relationship.Properties).Name);
        Assert.True(relationship.IsRequired);
        Assert.False(relationship.IsUnique);
        Assert.Equal(DeleteBehavior.Restrict, relationship.DeleteBehavior);
        Assert.Equal("Category", relationship.DependentToPrincipal.Name);
        Assert.Equal("Products", relationship.PrincipalToDependent.Name);
    }

    [Theory]
    [InlineData(typeof(User), "Email")]
    [InlineData(typeof(Product), "ProductName")]
    public void UniqueIndex_EnforcesBusinessUniqueness(Type type, string property)
    {
        using var context = CreateContext();

        Assert.Contains(context.Model.FindEntityType(type).GetIndexes(),
            index => index.IsUnique && index.Properties.Count == 1 && index.Properties[0].Name == property);
    }

    [Fact]
    public void RelationshipMigration_AddsForeignKeyWithoutDeletingData()
    {
        using var context = CreateContext();

        var sql = context.GetService<IMigrator>().GenerateScript("20260117130843_First", "20261003131931_AddProductCategoryRelationship");

        Assert.Contains("FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([CategoryId])", sql);
        Assert.Contains("CREATE INDEX [IX_Products_CategoryId]", sql);
        Assert.DoesNotContain("ON DELETE CASCADE", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
        Assert.DoesNotContain("DROP COLUMN", sql);
    }

    [Fact]
    public void PrimaryKeyMigration_RenamesColumnsWithoutDeletingData()
    {
        using var context = CreateContext();

        var sql = context.GetService<IMigrator>().GenerateScript("20261003132601_AddProductCategoryNavigations", "20261003134122_RenamePrimaryKeyColumnsToId");

        Assert.Contains("sp_rename N'[Products].[ProductId]', N'Id', N'COLUMN'", sql);
        Assert.Contains("sp_rename N'[Categories].[CategoryId]', N'Id', N'COLUMN'", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
        Assert.DoesNotContain("DROP COLUMN", sql);
    }

    // Inspecting metadata and generating SQL does not open a database connection.
    private static NorthwindContext CreateContext() => new(new DbContextOptionsBuilder<NorthwindContext>()
        .UseSqlServer("Server=localhost;Database=Regression;Integrated Security=True;TrustServerCertificate=True").Options);
}
