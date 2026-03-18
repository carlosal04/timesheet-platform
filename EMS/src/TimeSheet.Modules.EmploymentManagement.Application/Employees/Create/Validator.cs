using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;

namespace TimeSheet.Modules.EmploymentManagement.Application.Employees.Create;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Status)
            .Must(value => value is EmployeeStatusCodes.Active or EmployeeStatusCodes.Inactive)
            .WithMessage("status must be Active or Inactive.");

        RuleFor(x => x.DateOfBirth)
            .Must(BeAtLeastTwentyOneYearsOld)
            .WithMessage("dateOfBirth must represent an employee who is 21 years old or older on the current date.");

        RuleFor(x => x)
            .Must(x => x.HireDate >= x.DateOfBirth.AddYears(14))
            .WithMessage("hireDate must not be earlier than dateOfBirth + 14 years.");

        When(
            x => x.Addresses is not null,
            () => RuleForEach(x => x.Addresses!)
                .SetValidator(new AddressValidator()));
    }

    private static bool BeAtLeastTwentyOneYearsOld(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return dateOfBirth.AddYears(21) <= today;
    }

    private sealed class AddressValidator : AbstractValidator<Address>
    {
        public AddressValidator()
        {
            RuleFor(x => x.AddressType)
                .Must(value => value is AddressTypeCodes.Home
                    or AddressTypeCodes.Mailing
                    or AddressTypeCodes.EmergencyContact
                    or AddressTypeCodes.Other)
                .WithMessage("addressType must be a canonical address type.");

            RuleFor(x => x.Line1)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Line2)
                .MaximumLength(200);

            RuleFor(x => x.City)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.State)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.ZipCode)
                .NotEmpty()
                .MaximumLength(20);

            RuleFor(x => x.CountryCode)
                .NotEmpty()
                .Length(2);
        }
    }
}
