using FluentValidation;
using HomeManager.Application.DTOs.Storage;

namespace HomeManager.Application.Validators.Storage;

public class UpdateStorageValidator : AbstractValidator<UpdateStorageDto>
{
    public UpdateStorageValidator()
    {
    }
}
