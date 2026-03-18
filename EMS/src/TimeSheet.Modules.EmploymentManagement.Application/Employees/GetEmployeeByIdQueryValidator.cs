using FluentValidation;

namespace TimeSheet.Modules.EmploymentManagement.Application.Employees;

public sealed class GetEmployeeByIdQueryValidator : AbstractValidator<GetEmployeeByIdQuery>
{
    public GetEmployeeByIdQueryValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}
