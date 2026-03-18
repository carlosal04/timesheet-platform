using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;

namespace TimeSheet.Modules.EmploymentManagement.Application.Employees;

public sealed class ListEmployeesQueryValidator : AbstractValidator<ListEmployeesQuery>
{
    public ListEmployeesQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrWhiteSpace(status) ||
                            status == EmployeeStatusCodes.Active ||
                            status == EmployeeStatusCodes.Inactive)
            .WithMessage("Status must be Active or Inactive when provided.");

        RuleFor(x => x)
            .Must(x => !x.HireDateFrom.HasValue || !x.HireDateTo.HasValue || x.HireDateFrom.Value <= x.HireDateTo.Value)
            .WithMessage("hireDateFrom must be earlier than or equal to hireDateTo.");
    }
}
