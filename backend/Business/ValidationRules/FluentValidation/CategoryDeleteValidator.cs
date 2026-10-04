using Entities.Concrete;
using FluentValidation;

namespace Business.ValidationRules.FluentValidation
{
    public class CategoryDeleteValidator : AbstractValidator<Category>
    {
        public CategoryDeleteValidator()
        {
            RuleFor(c => c.Id).GreaterThan(0);
        }
    }
}
