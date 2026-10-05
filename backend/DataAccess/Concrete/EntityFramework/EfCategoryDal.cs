using System;
using System.Collections.Generic;
using System.Text;
using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework
{
    public class EfCategoryDal : EfEntityRepositoryBase<Category, NorthwindContext>, ICategoryDal
    {
        public bool Exists(int categoryId) => _context.Categories.Any(c => c.Id == categoryId);

		public EfCategoryDal(NorthwindContext context) : base(context)
		{
		}
	}
}
