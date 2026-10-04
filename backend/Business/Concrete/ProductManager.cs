using Business.Abstract;
using Business.Constants;
using Business.ValidationRules.FluentValidation;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.Aspects.Autofac.Transaction;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Log4Net.Loggers;
using Core.Utilities.Business;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Business.BusinessAspects.Autofac;

namespace Business.Concrete
{
	public class ProductManager : IProductService
	{
		private IProductDal _productDal;
		private ICategoryService _categoryService;

		public ProductManager(IProductDal productDal, ICategoryService categoryService)
		{
			_productDal = productDal;
			_categoryService = categoryService;
		}

		[SecuredOperation("Admin,Product.Get", Priority = -100)]
		[LogAspect(typeof(FileLogger))]
		public IDataResult<Product> GetById(int productId)
		{
			return new SuccessDataResult<Product>(_productDal.GetWithCategory(productId));
		}

		[SecuredOperation("Admin,Product.Get", Priority = -100)]
		[LogAspect(typeof(DatabaseLogger))]
		[PerformanceAspect(5)]
		public IDataResult<List<Product>> GetList()
		{
			return new SuccessDataResult<List<Product>>(_productDal.GetListWithCategory());
		}

		[SecuredOperation("Admin,Product.Get", Priority = -100)]
		[LogAspect(typeof(DatabaseLogger))]
		[CacheAspect(duration: 10)]
		public IDataResult<List<Product>> GetListByCategory(int categoryId)
		{
			return new SuccessDataResult<List<Product>>(_productDal.GetListWithCategory(categoryId));
		}

		[SecuredOperation("Admin,Product.Add", Priority = -100)]
		[ValidationAspect(typeof(ProductValidator), Priority = 1)]
		[CacheRemoveAspect("IProductService.Get")]
		public IResult Add(Product product)
		{
			IResult result = BusinessRules.Run(CheckIfProductNameExists(product.ProductName), CheckIfCategoryExists(product.CategoryId));

			if (result != null)
			{
				return result;
			}
			_productDal.Add(product);
			return new SuccessResult(Messages.ProductAdded);
		}

		private IResult CheckIfProductNameExists(string productName, int? excludedProductId = null)
		{

			var result = _productDal.ProductNameExists(productName, excludedProductId);
			if (result)
			{
				return new ErrorResult(Messages.ProductNameAlreadyExists);
			}

			return new SuccessResult();
		}

		private IResult CheckIfCategoryExists(int categoryId)
        {
            var categories = _categoryService.GetList();
            return categories.Data.Any(c => c.Id == categoryId)
                ? new SuccessResult()
                : new ErrorResult("Category does not exist.");
        }

		[SecuredOperation("Admin,Product.Delete", Priority = -100)]
		[CacheRemoveAspect("IProductService.Get")]
		public IResult Delete(Product product)
		{
			var existing = _productDal.Get(p => p.Id == product.Id);
            if (existing == null) return new ErrorResult("Product does not exist.");
            _productDal.Delete(existing);
			return new SuccessResult(Messages.ProductDeleted);
		}

		[SecuredOperation("Admin,Product.Update", Priority = -100)]
		[ValidationAspect(typeof(ProductValidator), Priority = 1)]
		[CacheRemoveAspect("IProductService.Get")]
		public IResult Update(Product product)
		{

            var existing = _productDal.Get(p => p.Id == product.Id);
            if (existing == null) return new ErrorResult("Product does not exist.");
            var result = BusinessRules.Run(
                CheckIfCategoryExists(product.CategoryId),
                CheckIfProductNameExists(product.ProductName, product.Id));
            if (result != null) return result;

            ApplyProductChanges(existing, product);
            _productDal.Update(existing);
            return new SuccessResult(Messages.ProductUpdated);
		}

        private static void ApplyProductChanges(Product existing, Product requested)
        {
            existing.ProductName = requested.ProductName;
            existing.CategoryId = requested.CategoryId;
            existing.QuantityPerUnit = requested.QuantityPerUnit;
            existing.UnitPrice = requested.UnitPrice;
            existing.UnitsInStock = requested.UnitsInStock;
        }

		[SecuredOperation("Admin", Priority = -100)]
		[ValidationAspect(typeof(ProductValidator), Priority = 1)]
		[TransactionScopeAspect]
		[CacheRemoveAspect("IProductService.Get")]
		public IResult TransactionalOperation(Product product)
		{
            // Update performs record and business-rule checks before persisting.
            return Update(product);
		}
	}
}
