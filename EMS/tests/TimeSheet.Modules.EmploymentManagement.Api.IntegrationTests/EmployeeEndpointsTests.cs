using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Application.Authentication;
using TimeSheet.Modules.EmploymentManagement.Application.Employees;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class EmployeeEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public EmployeeEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEmployees_ReturnsVisibleEmployeesAndOptionalPrimaryAddress_ForAdmin()
    {
        await _factory.ResetDatabaseAsync();

        var visibleEmployeeId = Guid.NewGuid();
        var primaryAddressId = Guid.NewGuid();

        await _factory.SeedAsync(async dbContext =>
        {
            dbContext.Employees.AddRange(
                new Employee
                {
                    Id = visibleEmployeeId,
                    FirstName = "Ana",
                    LastName = "Lopez",
                    Email = "ana.lopez@company.com",
                    Phone = "5551234567",
                    DateOfBirth = new DateOnly(1990, 6, 18),
                    HireDate = new DateOnly(2025, 1, 15),
                    Status = EmployeeStatusCodes.Active,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                },
                new Employee
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Deleted",
                    LastName = "Employee",
                    Email = "deleted.employee@company.com",
                    Phone = "5550000000",
                    DateOfBirth = new DateOnly(1988, 3, 12),
                    HireDate = new DateOnly(2024, 2, 1),
                    Status = EmployeeStatusCodes.Inactive,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    DeletedAtUtc = DateTimeOffset.UtcNow
                });

            dbContext.EmployeeAddresses.AddRange(
                new EmployeeAddress
                {
                    Id = primaryAddressId,
                    EmployeeId = visibleEmployeeId,
                    AddressType = "Home",
                    IsPrimary = true,
                    Line1 = "100 Main St",
                    City = "Pittsburgh",
                    State = "PA",
                    ZipCode = "15222",
                    CountryCode = "US",
                    CreatedAtUtc = DateTimeOffset.UtcNow
                },
                new EmployeeAddress
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = visibleEmployeeId,
                    AddressType = "Mailing",
                    IsPrimary = false,
                    Line1 = "200 Oak Ave",
                    Line2 = "Apt 4",
                    City = "Pittsburgh",
                    State = "PA",
                    ZipCode = "15213",
                    CountryCode = "US",
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1)
                });

            await Task.CompletedTask;
        });

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/employees?includePrimaryAddress=true");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResult<EmployeeSummaryView>>();
        Assert.NotNull(payload);
        Assert.Single(payload!.Items);
        Assert.Equal(1, payload.TotalCount);
        Assert.Equal(visibleEmployeeId, payload.Items[0].Id);
        Assert.NotNull(payload.Items[0].PrimaryAddress);
        Assert.Equal(primaryAddressId, payload.Items[0].PrimaryAddress!.Id);
    }

    [Fact]
    public async Task GetEmployeeById_ReturnsAddressesPrimaryFirst_ForBasic()
    {
        await _factory.ResetDatabaseAsync();

        var basicUserId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var primaryAddressId = Guid.NewGuid();
        var secondaryAddressId = Guid.NewGuid();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var basicRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Basic);

            var user = new User
            {
                Id = basicUserId,
                Email = "basic@example.com",
                RoleId = basicRole.Id,
                EmployeeId = employeeId,
                IsActive = true
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, "P@ssw0rd123!");

            dbContext.Users.Add(user);
            dbContext.Employees.Add(new Employee
            {
                Id = employeeId,
                FirstName = "Bea",
                LastName = "Stone",
                Email = "bea.stone@company.com",
                Phone = "5551112222",
                DateOfBirth = new DateOnly(1991, 2, 10),
                HireDate = new DateOnly(2024, 4, 1),
                Status = EmployeeStatusCodes.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            dbContext.EmployeeAddresses.AddRange(
                new EmployeeAddress
                {
                    Id = secondaryAddressId,
                    EmployeeId = employeeId,
                    AddressType = "Mailing",
                    IsPrimary = false,
                    Line1 = "200 Oak Ave",
                    City = "Pittsburgh",
                    State = "PA",
                    ZipCode = "15213",
                    CountryCode = "US",
                    CreatedAtUtc = DateTimeOffset.UtcNow
                },
                new EmployeeAddress
                {
                    Id = primaryAddressId,
                    EmployeeId = employeeId,
                    AddressType = "Home",
                    IsPrimary = true,
                    Line1 = "100 Main St",
                    City = "Pittsburgh",
                    State = "PA",
                    ZipCode = "15222",
                    CountryCode = "US",
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1)
                },
                new EmployeeAddress
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employeeId,
                    AddressType = "Old",
                    IsPrimary = false,
                    Line1 = "300 Hidden Rd",
                    City = "Pittsburgh",
                    State = "PA",
                    ZipCode = "15210",
                    CountryCode = "US",
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(2),
                    DeletedAtUtc = DateTimeOffset.UtcNow.AddMinutes(3)
                });

            await dbContext.SaveChangesAsync();
        });

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var authCookie = await LoginAsync(client, "basic@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/employees/{employeeId}");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<EmployeeDetailView>();
        Assert.NotNull(payload);
        Assert.Equal(employeeId, payload!.Id);
        Assert.Equal(2, payload.Addresses.Count);
        Assert.Equal(primaryAddressId, payload.Addresses[0].Id);
        Assert.Equal(secondaryAddressId, payload.Addresses[1].Id);
    }

    [Fact]
    public async Task GetEmployees_IncludeDeletedIsForbidden_ForBasic()
    {
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var basicRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Basic);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "basic.reader@example.com",
                RoleId = basicRole.Id,
                IsActive = true
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, "P@ssw0rd123!");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var authCookie = await LoginAsync(client, "basic.reader@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/employees?includeDeleted=true");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEmployees_WithInvalidQuery_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/employees?page=0&pageSize=101&status=Unknown");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return ExtractCookie(response);
    }

    private static string ExtractCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(AuthCookieName + "=", StringComparison.OrdinalIgnoreCase));

        return header.Split(';', 2, StringSplitOptions.TrimEntries)[0];
    }
}
