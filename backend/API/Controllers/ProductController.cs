using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business.Abstract;
using Core.Extensions;
using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ProductsController : ControllerBase
	{
		private IProductService _productService;

		public ProductsController(IProductService productService)
		{
			_productService = productService;
		}

		[HttpGet("getall")]
		public IActionResult GetList()
		{

			var result = _productService.GetList();
			if (result.Success)
			{
				return Ok(result.Data.Select(ToResponse).ToList());
			}

			return BadRequest(result.Message);
		}

		[HttpGet("getlistbycategory")]
		public IActionResult GetListByCategory(int categoryId)
		{
			var result = _productService.GetListByCategory(categoryId);
			if (result.Success)
			{
				return Ok(result.Data.Select(ToResponse).ToList());
			}

			return BadRequest(result.Message);
		}

		[HttpGet("getbyid")]
		public IActionResult GetById(int productId)
		{
			var result = _productService.GetById(productId);
			if (result.Success)
			{
				return result.Data == null ? NotFound("Product does not exist.") : Ok(ToResponse(result.Data));
			}

			return BadRequest(result.Message);
		}

        private static ProductResponseDto ToResponse(Product product)
        {
            return new ProductResponseDto
            {
                Id = product.Id,
                ProductName = product.ProductName,
                CategoryId = product.CategoryId,
                Category = product.Category == null ? null : new CategoryResponseDto
                {
                    Id = product.Category.Id,
                    CategoryName = product.Category.CategoryName
                },
                QuantityPerUnit = product.QuantityPerUnit,
                UnitPrice = product.UnitPrice,
                UnitsInStock = product.UnitsInStock
            };
        }

		[HttpPost("add")]
		public IActionResult Add(ProductForCreateDto request)
        {
            var product = new Product
            {
                ProductName = request.ProductName,
                CategoryId = request.CategoryId,
                QuantityPerUnit = request.QuantityPerUnit,
                UnitPrice = request.UnitPrice,
                UnitsInStock = request.UnitsInStock
            };
			var result = _productService.Add(product);
			if (result.Success)
			{
				return Ok(result.Message);
			}

			return BadRequest(result.Message);
		}

		[HttpPost("update")]
		public IActionResult Update(ProductForUpdateDto request)
        {
            var product = new Product
            {
                Id = request.Id,
                ProductName = request.ProductName,
                CategoryId = request.CategoryId,
                QuantityPerUnit = request.QuantityPerUnit,
                UnitPrice = request.UnitPrice,
                UnitsInStock = request.UnitsInStock
            };
			var result = _productService.Update(product);
			if (result.Success)
			{
				return Ok(result.Message);
			}

			return BadRequest(result.Message);
		}

		[HttpDelete("{id:int}")]
		public IActionResult Delete([FromRoute] int id)
        {
            var product = new Product { Id = id };
			var result = _productService.Delete(product);
			if (result.Success)
			{
				return Ok(result.Message);
			}

			return BadRequest(result.Message);
		}

		[HttpPost("transaction")]
		public IActionResult TransactionTest(ProductForUpdateDto request)
        {
            var product = new Product
            {
                Id = request.Id,
                ProductName = request.ProductName,
                CategoryId = request.CategoryId,
                QuantityPerUnit = request.QuantityPerUnit,
                UnitPrice = request.UnitPrice,
                UnitsInStock = request.UnitsInStock
            };
			var result = _productService.TransactionalOperation(product);
			if (result.Success)
			{
				return Ok(result.Message);
			}

			return BadRequest(result.Message);
		}

	}
}