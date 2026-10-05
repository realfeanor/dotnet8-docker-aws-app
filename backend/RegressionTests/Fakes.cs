using System.Linq.Expressions;
using Business.Abstract;
using Core.Entities.Concrete;
using DataAccess.Abstract;
using Entities.Concrete;

namespace RegressionTests;

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

