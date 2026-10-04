using Core.Entities;

namespace Entities.Dtos
{
    public class CategoryResponseDto : IDto
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
