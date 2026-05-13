using FluentValidation;
using HomeManager.Application.DTOs.Product;

namespace HomeManager.Application.Validators.Product;

public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
    }
}
