using Core.DataAccess;
using Entities.Concrete;
using System.Collections.Generic;

namespace DataAccess.Abstract
{
    public interface IProductDal : IEntityRepository<Product>
    {
        Product GetWithCategory(int productId);
        List<Product> GetListWithCategory(int? categoryId = null);
        bool ProductNameExists(string productName, int? excludedProductId = null);
        bool HasProductsInCategory(int categoryId);
    }
}
