using System.Collections.Generic;
using System.Linq;
using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Concrete.EntityFramework
{
    public class EfProductDal : EfEntityRepositoryBase<Product, NorthwindContext>, IProductDal
    {
        public EfProductDal(NorthwindContext context) : base(context) { }

        public Product GetWithCategory(int productId) =>
            _context.Products.AsNoTracking().Include(p => p.Category).SingleOrDefault(p => p.Id == productId);

        public List<Product> GetListWithCategory(int? categoryId = null)
        {
            var query = _context.Products.AsNoTracking().Include(p => p.Category).AsQueryable();
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
            return query.ToList();
        }

        public bool ProductNameExists(string productName, int? excludedProductId = null) =>
            _context.Products.Any(p => p.ProductName == productName && (!excludedProductId.HasValue || p.Id != excludedProductId.Value));
        public bool HasProductsInCategory(int categoryId) => _context.Products.Any(p => p.CategoryId == categoryId);
    }
}
