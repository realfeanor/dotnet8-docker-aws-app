using Business.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ProductController : ControllerBase
	{
		private IProductService _productService;

		public ProductController(IProductService productService)
		{
			_productService = productService;
		}

		[HttpPost("addproduct")]
		public IActionResult AddProduct(Product product)
		{
			//entrance.PersonType = null;
			var result = _productService.Add(product);
			if (result.Success)
			{
				return Ok(result);
			}
			return BadRequest(result);
		}

		[HttpGet("listallproducts")]
		public IActionResult ListAllProducts()
		{
			var result = _productService.GetList();
			if (result.Success)
			{
				return Ok(result);
			}
			return BadRequest(result);
		}

		[HttpGet("listproductsbycategory")]
		public IActionResult ListAllProducts(int categoryId)
		{
			var result = _productService.GetListByCategory(categoryId);
			if (result.Success)
			{
				return Ok(result);
			}
			return BadRequest(result);
		}
	}
}
