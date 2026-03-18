using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;

namespace TimeSheet.Modules.EmploymentManagement.Application.Addresses.Update;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.AddressId).NotEmpty();
        RuleFor(x => x.AddressType)
            .Must(value => value is AddressTypeCodes.Home
                or AddressTypeCodes.Mailing
                or AddressTypeCodes.EmergencyContact
                or AddressTypeCodes.Other)
            .WithMessage("addressType must be a canonical address type.");
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Line2).MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ZipCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.CountryCode).NotEmpty().Length(2);
    }
}
