using FluentValidation;
using HomeManager.Application.DTOs.Product;

namespace HomeManager.Application.Validators.Product;

public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductValidator()
    {
    }
}
