using Core.Entities;

namespace Entities.Dtos
{
    public class ProductForUpdateDto : IDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string QuantityPerUnit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public short UnitsInStock { get; set; }
    }
}
