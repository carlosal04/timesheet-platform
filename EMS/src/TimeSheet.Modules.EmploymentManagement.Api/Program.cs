using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using TimeSheet.Modules.EmploymentManagement.Api.Authentication;
using TimeSheet.Modules.EmploymentManagement.Api.Authorization;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Addresses;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Common;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Employees;
using TimeSheet.Modules.EmploymentManagement.Api.Infrastructure;
using CreateEmployeeAddressCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Create.Command;
using CreateEmployeeAddressResult = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Create.Result;
using DeleteEmployeeAddressCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Delete.Command;
using LoginCommand = TimeSheet.Modules.EmploymentManagement.Application.Authentication.Login.Command;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication.Configuration;
using LoginResult = TimeSheet.Modules.EmploymentManagement.Application.Authentication.Login.Result;
using LogoutCommand = TimeSheet.Modules.EmploymentManagement.Application.Authentication.Logout.Command;
using GetEmployeeAddressByIdQuery = TimeSheet.Modules.EmploymentManagement.Application.Addresses.GetById.Query;
using GetEmployeeAddressByIdResult = TimeSheet.Modules.EmploymentManagement.Application.Addresses.GetById.Result;
using ListEmployeeAddressesQuery = TimeSheet.Modules.EmploymentManagement.Application.Addresses.List.Query;
using ListEmployeeAddressesResult = TimeSheet.Modules.EmploymentManagement.Application.Addresses.List.Result;
using ListMyEmployeeAddressesQuery = TimeSheet.Modules.EmploymentManagement.Application.Addresses.ListMine.Query;
using SetMyEmployeeAddressPrimaryCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.SetOwnPrimary.Command;
using SetEmployeeAddressPrimaryCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.SetPrimary.Command;
using UpdateEmployeeAddressCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Update.Command;
using UpdateEmployeeAddressResult = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Update.Result;
using CreateEmployeeCommand = TimeSheet.Modules.EmploymentManagement.Application.Employees.Create.Command;
using CreateEmployeeAddress = TimeSheet.Modules.EmploymentManagement.Application.Employees.Create.Address;
using CreateEmployeeResult = TimeSheet.Modules.EmploymentManagement.Application.Employees.Create.Result;
using DeleteEmployeeCommand = TimeSheet.Modules.EmploymentManagement.Application.Employees.Delete.Command;
using GetEmployeeByIdQuery = TimeSheet.Modules.EmploymentManagement.Application.Employees.GetById.Query;
using GetEmployeeByIdResult = TimeSheet.Modules.EmploymentManagement.Application.Employees.GetById.Result;
using ListEmployeesQuery = TimeSheet.Modules.EmploymentManagement.Application.Employees.List.Query;
using ListEmployeesResult = TimeSheet.Modules.EmploymentManagement.Application.Employees.List.Result;
using UpdateEmployeeCommand = TimeSheet.Modules.EmploymentManagement.Application.Employees.Update.Command;
using UpdateEmployeeResult = TimeSheet.Modules.EmploymentManagement.Application.Employees.Update.Result;
using TimeSheet.Modules.EmploymentManagement.Application;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
var dataProtectionKeysPath = Path.GetFullPath(Path.Combine(
    builder.Environment.ContentRootPath,
    "..",
    "..",
    ".local",
    "data-protection-keys"));

Directory.CreateDirectory(dataProtectionKeysPath);

builder.Host.UseWolverine(options =>
{
    options.Discovery.IncludeAssembly(typeof(TimeSheet.Modules.EmploymentManagement.Application.AssemblyMarker).Assembly);
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppProblemExceptionHandler>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddEmploymentManagementApplication();
builder.Services.AddEmploymentManagementInfrastructure(builder.Configuration);
builder.Services.AddScoped<AuthSessionCookieEvents>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationMiddlewareResultHandler>();

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
    .AddPolicy(PolicyNames.AuthenticatedUser, policy => policy.RequireAuthenticatedUser())
    .AddPolicy(PolicyNames.AdminOnly, policy => policy.RequireRole(RoleCodes.Admin))
    .AddPolicy(PolicyNames.EmployeeRead, policy => policy.RequireRole(RoleCodes.Admin, RoleCodes.Basic))
    .AddPolicy(PolicyNames.EmployeeWrite, policy => policy.RequireRole(RoleCodes.Admin))
    .AddPolicy(PolicyNames.EmployeeDelete, policy => policy.RequireRole(RoleCodes.Admin))
    .AddPolicy(PolicyNames.AddressRead, policy => policy.RequireRole(RoleCodes.Admin, RoleCodes.Basic))
    .AddPolicy(PolicyNames.AddressWrite, policy => policy.RequireRole(RoleCodes.Admin))
    .AddPolicy(PolicyNames.AddressDeleteAny, policy => policy.RequireRole(RoleCodes.Admin))
    .AddPolicy(PolicyNames.AddressPrimaryManageAny, policy => policy.RequireRole(RoleCodes.Admin))
    .AddPolicy(PolicyNames.OwnAddressDelete, policy => policy.RequireRole(RoleCodes.Basic))
    .AddPolicy(PolicyNames.OwnAddressPrimaryManage, policy => policy.RequireRole(RoleCodes.Basic))
    .AddPolicy(PolicyNames.RoleRead, policy => policy.RequireRole(RoleCodes.Admin))
    .AddPolicy(PolicyNames.UserRoleAssign, policy => policy.RequireRole(RoleCodes.Admin))
    .AddPolicy(PolicyNames.AuditLogRead, policy => policy.RequireRole(RoleCodes.Admin))
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
        var statusCode = result.ErrorCode == "locked_out"
            ? StatusCodes.Status423Locked
            : StatusCodes.Status401Unauthorized;

        var title = result.ErrorCode == "locked_out"
            ? "Account is temporarily locked"
            : "Invalid credentials";

        return TypedResults.Problem(statusCode: statusCode, title: title);
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
        new(ClaimTypes.Email, result.User.Email),
        new(ClaimTypes.Role, result.RoleCode),
        new(CustomClaimTypes.SessionId, result.Session.Id.ToString())
    };

    if (result.EmployeeId.HasValue)
    {
        claims.Add(new Claim(CustomClaimTypes.EmployeeId, result.EmployeeId.Value.ToString()));
    }

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
    var sessionIdValue = principal.FindFirstValue(CustomClaimTypes.SessionId);
    if (Guid.TryParse(sessionIdValue, out var sessionId))
    {
        await bus.InvokeAsync(new LogoutCommand(sessionId));
    }

    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return TypedResults.NoContent();
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

    var result = await bus.InvokeAsync<ListEmployeesResult>(
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
}).RequireAuthorization(PolicyNames.EmployeeRead);

app.MapGet("/employees/{id:guid}", async Task<IResult> (
    Guid id,
    IMessageBus bus,
    CancellationToken cancellationToken) =>
{
    var result = await bus.InvokeAsync<GetEmployeeByIdResult>(new GetEmployeeByIdQuery(id));
    return result.Employee is null ? TypedResults.NotFound() : TypedResults.Ok(result.Employee);
}).RequireAuthorization(PolicyNames.EmployeeRead);

app.MapGet("/employees/{employeeId:guid}/addresses", async Task<IResult> (
    Guid employeeId,
    ClaimsPrincipal principal,
    IMessageBus bus,
    bool? includeDeleted,
    CancellationToken cancellationToken) =>
{
    var includeDeletedValue = includeDeleted ?? false;
    if (includeDeletedValue && !principal.IsInRole(RoleCodes.Admin))
    {
        return TypedResults.Forbid();
    }

    var result = await bus.InvokeAsync<ListEmployeeAddressesResult>(
        new ListEmployeeAddressesQuery(employeeId, includeDeletedValue));

    return TypedResults.Ok(result);
}).RequireAuthorization(PolicyNames.AddressRead);

app.MapGet("/employees/{employeeId:guid}/addresses/{addressId:guid}", async Task<IResult> (
    Guid employeeId,
    Guid addressId,
    IMessageBus bus,
    CancellationToken cancellationToken) =>
{
    var result = await bus.InvokeAsync<GetEmployeeAddressByIdResult>(
        new GetEmployeeAddressByIdQuery(employeeId, addressId));

    return result.Address is null ? TypedResults.NotFound() : TypedResults.Ok(result.Address);
}).RequireAuthorization(PolicyNames.AddressRead);

app.MapPost("/employees/{employeeId:guid}/addresses", async Task<IResult> (
    Guid employeeId,
    UpsertAddressRequest request,
    IMessageBus bus,
    CancellationToken cancellationToken) =>
{
    var result = await bus.InvokeAsync<CreateEmployeeAddressResult>(
        new CreateEmployeeAddressCommand(
            employeeId,
            request.AddressType,
            request.IsPrimary,
            request.Line1,
            request.Line2,
            request.City,
            request.State,
            request.ZipCode,
            request.CountryCode));

    return TypedResults.Created(
        $"/employees/{result.EmployeeId}/addresses/{result.Id}",
        new AddressWriteResponse(result.Id, result.EmployeeId));
}).RequireAuthorization(PolicyNames.AddressWrite);

app.MapPut("/employees/{employeeId:guid}/addresses/{addressId:guid}", async Task<IResult> (
    Guid employeeId,
    Guid addressId,
    UpsertAddressRequest request,
    IMessageBus bus,
    CancellationToken cancellationToken) =>
{
    var result = await bus.InvokeAsync<UpdateEmployeeAddressResult>(
        new UpdateEmployeeAddressCommand(
            employeeId,
            addressId,
            request.AddressType,
            request.IsPrimary,
            request.Line1,
            request.Line2,
            request.City,
            request.State,
            request.ZipCode,
            request.CountryCode));

    return TypedResults.Ok(new AddressWriteResponse(result.Id, result.EmployeeId));
}).RequireAuthorization(PolicyNames.AddressWrite);

app.MapMethods("/employees/{employeeId:guid}/addresses/{addressId:guid}/primary", ["PATCH"], async Task<IResult> (
    Guid employeeId,
    Guid addressId,
    IMessageBus bus,
    CancellationToken cancellationToken) =>
{
    await bus.InvokeAsync(new SetEmployeeAddressPrimaryCommand(employeeId, addressId));
    return TypedResults.NoContent();
}).RequireAuthorization(PolicyNames.AddressPrimaryManageAny);

app.MapDelete("/employees/{employeeId:guid}/addresses/{addressId:guid}", async Task<IResult> (
    Guid employeeId,
    Guid addressId,
    IMessageBus bus,
    CancellationToken cancellationToken) =>
{
    await bus.InvokeAsync(new DeleteEmployeeAddressCommand(employeeId, addressId));
    return TypedResults.NoContent();
}).RequireAuthorization(PolicyNames.AddressDeleteAny);

app.MapGet("/me/addresses", async Task<IResult> (
    IMessageBus bus,
    CancellationToken cancellationToken) =>
{
    var result = await bus.InvokeAsync<ListEmployeeAddressesResult>(new ListMyEmployeeAddressesQuery());
    return TypedResults.Ok(result);
}).RequireAuthorization(PolicyNames.AddressRead);

app.MapMethods("/me/addresses/{addressId:guid}/primary", ["PATCH"], async Task<IResult> (
    Guid addressId,
    IMessageBus bus,
    CancellationToken cancellationToken) =>
{
    await bus.InvokeAsync(new SetMyEmployeeAddressPrimaryCommand(addressId));
    return TypedResults.NoContent();
}).RequireAuthorization(PolicyNames.OwnAddressPrimaryManage);

app.MapPost("/employees", async Task<IResult> (
    CreateEmployeeRequest request,
    IMessageBus bus,
    CancellationToken cancellationToken) =>
{
    var result = await bus.InvokeAsync<CreateEmployeeResult>(
        new CreateEmployeeCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.DateOfBirth,
            request.HireDate,
            request.Status,
            request.Addresses?.Select(address => new CreateEmployeeAddress(
                address.AddressType,
                address.IsPrimary,
                address.Line1,
                address.Line2,
                address.City,
                address.State,
                address.ZipCode,
                address.CountryCode)).ToArray()));

    return TypedResults.Created($"/employees/{result.Id}", new IdResponse(result.Id));
}).RequireAuthorization(PolicyNames.EmployeeWrite);

app.MapPut("/employees/{id:guid}", async Task<IResult> (
    Guid id,
    UpdateEmployeeRequest request,
    IMessageBus bus,
    CancellationToken cancellationToken) =>
{
    var result = await bus.InvokeAsync<UpdateEmployeeResult>(
        new UpdateEmployeeCommand(
            id,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.DateOfBirth,
            request.HireDate,
            request.Status));

    return TypedResults.Ok(new IdResponse(result.Id));
}).RequireAuthorization(PolicyNames.EmployeeWrite);

app.MapDelete("/employees/{id:guid}", async Task<IResult> (
    Guid id,
    IMessageBus bus,
    CancellationToken cancellationToken) =>
{
    await bus.InvokeAsync(new DeleteEmployeeCommand(id));
    return TypedResults.NoContent();
}).RequireAuthorization(PolicyNames.EmployeeDelete);

app.Run();

public partial class Program;
