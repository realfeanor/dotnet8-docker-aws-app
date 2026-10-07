using Business.Abstract;
using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos;
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
        public IActionResult AddCategory(CategoryForCreateDto request)
        {
            var category = new Category { CategoryName = request.CategoryName };
            var result = _categoryService.Add(category);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpPost("updatecategory")]
        public IActionResult UpdateCategory(CategoryForUpdateDto request)
        {
            var category = new Category { Id = request.Id, CategoryName = request.CategoryName };
            var result = _categoryService.Update(category);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpDelete("{id:int}")]
        public IActionResult DeleteCategory([FromRoute] int id)
        {
            var category = new Category { Id = id };
            var result = _categoryService.Delete(category);
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
                var categories = result.Data.Select(category => new CategoryResponseDto
                {
                    Id = category.Id,
                    CategoryName = category.CategoryName
                }).ToList();
                return Ok(new SuccessDataResult<List<CategoryResponseDto>>(categories, result.Message));
            }
            return BadRequest(result);
        }
    }
}
