using Business.Abstract;
using Core.Utilities.Results;
using Entities.Concrete;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CategoryController : ControllerBase
	{
		private ICategoryService _categoryService;

		public CategoryController(ICategoryService categoryService)
		{
			_categoryService = categoryService;
		}

		[HttpPost("addcategory")]
		public IActionResult AddCategory(Category category)
		{
			var result = _categoryService.Add(category);

			if (result.Success)
			{
				return Ok(result);
			}
			return BadRequest(result);
		}

		[HttpPost("getallcategories")]
		public IActionResult GetAllCategories()
		{
			var result = _categoryService.GetList();

			if (result.Success)
			{
				return Ok(result);
			}
			return BadRequest(result);
		}
	}
}
