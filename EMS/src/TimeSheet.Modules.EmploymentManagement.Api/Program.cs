using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSheet.Modules.EmploymentManagement.Api.Authentication;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Api.Infrastructure;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication;
using TimeSheet.Modules.EmploymentManagement.Application.Employees;
using TimeSheet.Modules.EmploymentManagement.Application;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWolverine(options =>
{
    options.Discovery.IncludeAssembly(typeof(TimeSheet.Modules.EmploymentManagement.Application.AssemblyMarker).Assembly);
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddEmploymentManagementApplication();
builder.Services.AddEmploymentManagementInfrastructure(builder.Configuration);
builder.Services.AddScoped<AuthSessionCookieEvents>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = builder.Configuration.GetSection(AuthOptions.SectionName).GetValue<string>("CookieName") ?? "ems.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.EventsType = typeof(AuthSessionCookieEvents);
        options.SlidingExpiration = false;
        options.LoginPath = "/auth/login";
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("EmployeeRead", policy => policy.RequireRole(RoleCodes.Admin, RoleCodes.Basic))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<TimeSheet.Modules.EmploymentManagement.Infrastructure.Initialization.DatabaseInitializer>();
    await initializer.InitializeAsync(CancellationToken.None);
}

app.MapGet("/", () => Results.Ok(new { module = "EMS", status = "ok" })).AllowAnonymous();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

app.MapPost("/auth/login", async Task<IResult> (
    LoginRequest request,
    IMessageBus bus,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var result = await bus.InvokeAsync<LoginResult>(new LoginCommand(request.Email, request.Password));
    if (!result.Succeeded || result.User is null || result.Session is null || result.RoleCode is null)
    {
        return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
        new(ClaimTypes.Email, result.User.Email),
        new(ClaimTypes.Role, result.RoleCode),
        new("session_id", result.Session.Id.ToString())
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = result.Session.ExpiresAtUtc
        });

    return TypedResults.Ok(new LoginResponse(result.User.Id, result.User.Email, result.RoleCode));
}).AllowAnonymous();

app.MapPost("/auth/logout", async Task<IResult> (
    ClaimsPrincipal principal,
    IMessageBus bus,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var sessionIdValue = principal.FindFirstValue("session_id");
    if (Guid.TryParse(sessionIdValue, out var sessionId))
    {
        await bus.InvokeAsync(new LogoutCommand(sessionId));
    }

    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return TypedResults.Ok();
});

app.MapGet("/auth/antiforgery", ([FromServices] IAntiforgery antiforgery, HttpContext httpContext) =>
{
    var tokens = antiforgery.GetAndStoreTokens(httpContext);
    return TypedResults.Ok(new AntiforgeryResponse("X-CSRF-TOKEN", tokens.RequestToken ?? string.Empty));
});

app.MapGet("/employees", async Task<IResult> (
    ClaimsPrincipal principal,
    IMessageBus bus,
    int? page,
    int? pageSize,
    string? name,
    string? status,
    DateOnly? hireDateFrom,
    DateOnly? hireDateTo,
    bool? includeDeleted,
    bool? includePrimaryAddress,
    CancellationToken cancellationToken) =>
{
    var includeDeletedValue = includeDeleted ?? false;
    if (includeDeletedValue && !principal.IsInRole(RoleCodes.Admin))
    {
        return TypedResults.Forbid();
    }

    var result = await bus.InvokeAsync<PagedResult<EmployeeSummaryView>>(
        new ListEmployeesQuery(
            page ?? 1,
            pageSize ?? 25,
            name,
            status,
            hireDateFrom,
            hireDateTo,
            includeDeletedValue,
            includePrimaryAddress ?? false));

    return TypedResults.Ok(result);
}).RequireAuthorization("EmployeeRead");

app.MapGet("/employees/{id:guid}", async Task<IResult> (
    Guid id,
    IMessageBus bus,
    CancellationToken cancellationToken) =>
{
    var result = await bus.InvokeAsync<GetEmployeeByIdResult>(new GetEmployeeByIdQuery(id));
    return result.Employee is null ? TypedResults.NotFound() : TypedResults.Ok(result.Employee);
}).RequireAuthorization("EmployeeRead");

app.Run();

public partial class Program;
