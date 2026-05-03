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
		private ICategoryDal _categoryDal;

		public CategoryManager(ICategoryDal categoryDal)
		{
			_categoryDal = categoryDal;
		}

		[SecuredOperation("Admin")]
		[ValidationAspect(typeof(CategoryValidator), Priority = 1)]
		public IResult Add(Category category)
		{
			return new SuccessResult(Messages.CategoryAdded);
		}

		public IDataResult<List<Category>> GetList()
		{
			return new SuccessDataResult<List<Category>>(_categoryDal.GetList().ToList());
		}
	}
}
