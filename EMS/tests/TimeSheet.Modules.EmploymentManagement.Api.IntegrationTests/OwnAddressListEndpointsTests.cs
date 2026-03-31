using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AddressListResult = TimeSheet.Modules.EmploymentManagement.Application.Addresses.List.Result;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class OwnAddressListEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public OwnAddressListEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyAddresses_ReturnsActiveAddressesPrimaryFirst()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        var primaryAddressId = Guid.NewGuid();
        var secondaryAddressId = Guid.NewGuid();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var developerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Developer);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "developer.selfservice@example.com",
                RoleId = developerRole.Id,
                EmployeeId = employeeId,
                IsActive = true
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, "P@ssw0rd123!");

            dbContext.Users.Add(user);
            dbContext.Employees.Add(new Employee
            {
                Id = employeeId,
                FirstName = "Ana",
                LastName = "Lopez",
                Email = "ana.lopez@company.com",
                Phone = "5551234567",
                DateOfBirth = new DateOnly(1990, 6, 18),
                HireDate = new DateOnly(2025, 1, 15),
                Status = EmployeeStatusCodes.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            dbContext.EmployeeAddresses.AddRange(
                new EmployeeAddress
                {
                    Id = secondaryAddressId,
                    EmployeeId = employeeId,
                    AddressType = AddressTypeCodes.Mailing,
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
                    AddressType = AddressTypeCodes.Home,
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
                    AddressType = AddressTypeCodes.Other,
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

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "developer.selfservice@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/me/addresses");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AddressListResult>();
        Assert.NotNull(payload);
        Assert.Equal(employeeId, payload!.EmployeeId);
        Assert.Equal(2, payload.Items.Count);
        Assert.Equal(primaryAddressId, payload.Items[0].Id);
        Assert.Equal(secondaryAddressId, payload.Items[1].Id);
    }

    [Fact]
    public async Task GetMyAddresses_WithoutLinkedEmployee_ReturnsForbidden()
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
                Email = "developer.nolink@example.com",
                RoleId = developerRole.Id,
                IsActive = true
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, "P@ssw0rd123!");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "developer.nolink@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/me/addresses");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(403, problem!.Status);
    }

    [Fact]
    public async Task GetMyAddresses_WhenLinkedEmployeeIsMissing_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var developerRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Developer);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "developer.missingemployee@example.com",
                RoleId = developerRole.Id,
                EmployeeId = employeeId,
                IsActive = true
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, "P@ssw0rd123!");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "developer.missingemployee@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/me/addresses");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
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
