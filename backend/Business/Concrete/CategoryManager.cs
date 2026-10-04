using Business.Abstract;
using Business.BusinessAspects.Autofac;
using Business.Constants;
using Business.ValidationRules.FluentValidation;
using Core.Aspects.Autofac.Validation;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Business.Concrete
{
	public class CategoryManager : ICategoryService
	{
		private readonly ICategoryDal _categoryDal;
        private readonly IProductDal _productDal;

		public CategoryManager(ICategoryDal categoryDal, IProductDal productDal)
		{
			_categoryDal = categoryDal;
            _productDal = productDal;
		}

		[SecuredOperation("Admin,Category.Add", Priority = -100)]
		[ValidationAspect(typeof(CategoryValidator), Priority = 1)]
		public IResult Add(Category category)
		{
			_categoryDal.Add(category);
			return new SuccessResult(Messages.CategoryAdded);
		}

        [SecuredOperation("Admin,Category.Update", Priority = -100)]
        [ValidationAspect(typeof(CategoryUpdateValidator), Priority = 1)]
        public IResult Update(Category category)
        {
            var existing = _categoryDal.Get(c => c.Id == category.Id);
            if (existing == null) return new ErrorResult(Messages.CategoryNotFound);
            ApplyCategoryChanges(existing, category);
            _categoryDal.Update(existing);
            return new SuccessResult(Messages.CategoryUpdated);
        }

        [SecuredOperation("Admin,Category.Delete", Priority = -100)]
        [ValidationAspect(typeof(CategoryDeleteValidator), Priority = 1)]
        public IResult Delete(Category category)
        {
            var existing = _categoryDal.Get(c => c.Id == category.Id);
            if (existing == null) return new ErrorResult(Messages.CategoryNotFound);
            var result = CheckIfCategoryHasProducts(category.Id);
            if (!result.Success) return result;
            _categoryDal.Delete(existing);
            return new SuccessResult(Messages.CategoryDeleted);
        }

        private IResult CheckIfCategoryHasProducts(int categoryId)
        {
            return !_productDal.HasProductsInCategory(categoryId)
                ? new SuccessResult()
                : new ErrorResult(Messages.CategoryHasProducts);
        }

        private static void ApplyCategoryChanges(Category existing, Category requested)
        {
            existing.CategoryName = requested.CategoryName;
        }

		[SecuredOperation("Admin,Category.Get", Priority = -100)]
		public IDataResult<List<Category>> GetList()
		{
			return new SuccessDataResult<List<Category>>(_categoryDal.GetList().ToList());
		}
	}
}
