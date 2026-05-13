using FluentValidation;
using HomeManager.Application.DTOs.Category;

namespace HomeManager.Application.Validators.Category;

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryValidator()
    {
    }
}
