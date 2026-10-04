using Core.Entities;

namespace Entities.Dtos
{
    public class CategoryForUpdateDto : IDto
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
