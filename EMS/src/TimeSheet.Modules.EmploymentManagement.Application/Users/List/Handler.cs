using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Users;

namespace TimeSheet.Modules.EmploymentManagement.Application.Users.List;

public sealed class Handler
{
    private readonly IUserReadService _userReadService;
    private readonly IValidator<Query> _validator;

    public Handler(IUserReadService userReadService, IValidator<Query> validator)
    {
        _userReadService = userReadService;
        _validator = validator;
    }

    public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        return await _userReadService.ListAsync(
            new Request(
                query.Page,
                query.PageSize,
                query.Email,
                query.RoleCode,
                query.IncludeInactive),
            cancellationToken);
    }
}
