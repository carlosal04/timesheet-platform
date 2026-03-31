using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EmployeeDetail = TimeSheet.Modules.EmploymentManagement.Application.Employees.GetById.Employee;
using EmployeeListResult = TimeSheet.Modules.EmploymentManagement.Application.Employees.List.Result;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Common;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Employees;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
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
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<EmployeeListResult>();
        Assert.NotNull(payload);
        Assert.Single(payload!.Items);
        Assert.Equal(1, payload.TotalCount);
        Assert.Equal(visibleEmployeeId, payload.Items[0].Id);
        Assert.NotNull(payload.Items[0].PrimaryAddress);
        Assert.Equal(primaryAddressId, payload.Items[0].PrimaryAddress!.Id);
    }

    [Fact]
    public async Task GetEmployeeById_ReturnsAddressesPrimaryFirst_ForManager()
    {
        await _factory.ResetDatabaseAsync();

        var managerUserId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var primaryAddressId = Guid.NewGuid();
        var secondaryAddressId = Guid.NewGuid();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var managerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Manager);

            var user = new User
            {
                Id = managerUserId,
                Email = "manager@example.com",
                RoleId = managerRole.Id,
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

        var authCookie = await LoginAsync(client, "manager@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/employees/{employeeId}");
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<EmployeeDetail>();
        Assert.NotNull(payload);
        Assert.Equal(employeeId, payload!.Id);
        Assert.Equal(2, payload.Addresses.Count);
        Assert.Equal(primaryAddressId, payload.Addresses[0].Id);
        Assert.Equal(secondaryAddressId, payload.Addresses[1].Id);
    }

    [Fact]
    public async Task GetEmployees_IncludeDeletedIsForbidden_ForManager()
    {
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var managerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Manager);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "manager.reader@example.com",
                RoleId = managerRole.Id,
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

        var authCookie = await LoginAsync(client, "manager.reader@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/employees?includeDeleted=true");
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEmployees_ForDeveloper_ReturnsForbidden()
    {
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var developerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Developer);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "developer.reader@example.com",
                RoleId = developerRole.Id,
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

        var authCookie = await LoginAsync(client, "developer.reader@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/employees");
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

    [Fact]
    public async Task CreateEmployee_CreatesEmployeeAndAuditEntries()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        var requestBody = new CreateEmployeeRequest(
            "Ava",
            "Jones",
            "ava.jones@company.com",
            "5553334444",
            new DateOnly(1990, 5, 9),
            new DateOnly(2024, 1, 15),
            EmployeeStatusCodes.Active,
            [
                new("Home", true, "100 Main St", null, "Pittsburgh", "PA", "15222", "US")
            ]);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/employees")
        {
            Content = JsonContent.Create(requestBody)
        };
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<IdResponse>();
        Assert.NotNull(payload);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var employee = await dbContext.Employees
                .Include(x => x.Addresses)
                .SingleAsync(x => x.Id == payload!.Id);

            Assert.Equal("ava.jones@company.com", employee.Email);
            Assert.Single(employee.Addresses);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.EmployeeCreated
                    && log.EntityId == employee.Id
                    && log.Result == AuditResults.Success);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.AddressCreated
                    && log.Result == AuditResults.Success);
        });
    }

    [Fact]
    public async Task CreateEmployee_ForManagerUser_ReturnsForbiddenProblemDetails()
    {
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var managerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Manager);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "manager.creator@example.com",
                RoleId = managerRole.Id,
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

        var authCookie = await LoginAsync(client, "manager.creator@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/employees")
        {
            Content = JsonContent.Create(new CreateEmployeeRequest(
                "Ava",
                "Jones",
                "ava.jones@company.com",
                "5553334444",
                new DateOnly(1990, 5, 9),
                new DateOnly(2024, 1, 15),
                EmployeeStatusCodes.Active,
                null))
        };
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status403Forbidden, problem!.Status);
    }

    [Fact]
    public async Task CreateEmployee_ForHrUser_ReturnsCreated()
    {
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var hrRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.HR);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "hr.creator@example.com",
                RoleId = hrRole.Id,
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

        var authCookie = await LoginAsync(client, "hr.creator@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/employees")
        {
            Content = JsonContent.Create(new CreateEmployeeRequest(
                "Harper",
                "Lee",
                "harper.lee@company.com",
                "5553334444",
                new DateOnly(1990, 5, 9),
                new DateOnly(2024, 1, 15),
                EmployeeStatusCodes.Active,
                null))
        };
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmployee_ForDeletedEmployee_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        await _factory.SeedAsync(async dbContext =>
        {
            dbContext.Employees.Add(new Employee
            {
                Id = employeeId,
                FirstName = "Ava",
                LastName = "Jones",
                Email = "ava.jones@company.com",
                Phone = "5553334444",
                DateOfBirth = new DateOnly(1990, 5, 9),
                HireDate = new DateOnly(2024, 1, 15),
                Status = EmployeeStatusCodes.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                DeletedAtUtc = DateTimeOffset.UtcNow
            });

            await Task.CompletedTask;
        });

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/employees/{employeeId}")
        {
            Content = JsonContent.Create(new UpdateEmployeeRequest(
                "Ava",
                "Stone",
                "ava.jones@company.com",
                "5559990000",
                new DateOnly(1990, 5, 9),
                new DateOnly(2024, 1, 15),
                EmployeeStatusCodes.Inactive))
        };
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEmployee_SoftDeletesAndSecondDeleteConflicts()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        await _factory.SeedAsync(async dbContext =>
        {
            dbContext.Employees.Add(new Employee
            {
                Id = employeeId,
                FirstName = "Ava",
                LastName = "Jones",
                Email = "ava.jones@company.com",
                Phone = "5553334444",
                DateOfBirth = new DateOnly(1990, 5, 9),
                HireDate = new DateOnly(2024, 1, 15),
                Status = EmployeeStatusCodes.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await Task.CompletedTask;
        });

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");

        using var firstRequest = new HttpRequestMessage(HttpMethod.Delete, $"/employees/{employeeId}");
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, firstRequest);

        var firstResponse = await client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var adminUserId = await dbContext.Users
                .Where(x => x.Email == "admin@example.com")
                .Select(x => x.Id)
                .SingleAsync();
            var employee = await dbContext.Employees.SingleAsync(x => x.Id == employeeId);
            Assert.NotNull(employee.DeletedAtUtc);
            Assert.Equal(adminUserId, employee.DeletedByUserId);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.EmployeeSoftDeleted
                    && log.EntityId == employeeId
                    && log.Result == AuditResults.Success);
        });

        using var secondRequest = new HttpRequestMessage(HttpMethod.Delete, $"/employees/{employeeId}");
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, secondRequest);

        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
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
