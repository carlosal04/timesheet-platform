using FluentValidation;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;

namespace TimeSheet.Modules.EmploymentManagement.Application.Authentication.GetSession;

public sealed class Handler
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISessionReadService _sessionReadService;
    private readonly IValidator<Query> _validator;

    public Handler(
        ICurrentUserContext currentUserContext,
        ISessionReadService sessionReadService,
        IValidator<Query> validator)
    {
        _currentUserContext = currentUserContext;
        _sessionReadService = sessionReadService;
        _validator = validator;
    }

    public async Task<Result?> Handle(Query query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        if (!_currentUserContext.IsAuthenticated
            || !_currentUserContext.UserId.HasValue
            || !_currentUserContext.SessionId.HasValue)
        {
            return null;
        }

        return await _sessionReadService.GetAsync(
            _currentUserContext.UserId.Value,
            _currentUserContext.SessionId.Value,
            cancellationToken);
    }
}
