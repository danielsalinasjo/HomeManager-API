using FluentValidation;
using HomeManager.Application.DTOs.Storage;

namespace HomeManager.Application.Validators.Storage;

public class CreateStorageValidator : AbstractValidator<CreateStorageDto>
{
    public CreateStorageValidator()
    {
    }
}
