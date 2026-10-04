using Core.Entities;

namespace Entities.Dtos
{
    public class CategoryForCreateDto : IDto
    {
        public string CategoryName { get; set; } = string.Empty;
    }
}
