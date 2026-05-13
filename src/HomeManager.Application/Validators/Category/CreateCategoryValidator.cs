using FluentValidation;
using HomeManager.Application.DTOs.Category;

namespace HomeManager.Application.Validators.Category;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryValidator()
    {
    }
}
