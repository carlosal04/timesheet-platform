using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Roles;

namespace TimeSheet.Modules.EmploymentManagement.Application.Roles.List;

public sealed class Handler
{
    private readonly IRoleReadService _roleReadService;
    private readonly IValidator<Query> _validator;

    public Handler(IRoleReadService roleReadService, IValidator<Query> validator)
    {
        _roleReadService = roleReadService;
        _validator = validator;
    }

    public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);
        return await _roleReadService.ListAsync(query, cancellationToken);
    }
}
