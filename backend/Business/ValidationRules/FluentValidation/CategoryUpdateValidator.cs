using Entities.Concrete;
using FluentValidation;

namespace Business.ValidationRules.FluentValidation
{
    public class CategoryUpdateValidator : AbstractValidator<Category>
    {
        public CategoryUpdateValidator()
        {
            Include(new CategoryValidator());
            RuleFor(c => c.Id).GreaterThan(0);
        }
    }
}
