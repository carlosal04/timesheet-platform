using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class OwnAddressSetPrimaryEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public OwnAddressSetPrimaryEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SetMyPrimaryAddress_ReplacesExistingPrimary()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        var existingPrimaryId = Guid.NewGuid();
        var newPrimaryId = Guid.NewGuid();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var basicRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Basic);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "basic.ownprimary@example.com",
                RoleId = basicRole.Id,
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
                    Id = existingPrimaryId,
                    EmployeeId = employeeId,
                    AddressType = AddressTypeCodes.Home,
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
                    Id = newPrimaryId,
                    EmployeeId = employeeId,
                    AddressType = AddressTypeCodes.Mailing,
                    IsPrimary = false,
                    Line1 = "200 Oak Ave",
                    City = "Pittsburgh",
                    State = "PA",
                    ZipCode = "15213",
                    CountryCode = "US",
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1)
                });

            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "basic.ownprimary@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/me/addresses/{newPrimaryId}/primary");
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var addresses = await dbContext.EmployeeAddresses
                .Where(x => x.EmployeeId == employeeId && x.DeletedAtUtc == null)
                .ToListAsync();

            Assert.False(addresses.Single(x => x.Id == existingPrimaryId).IsPrimary);
            Assert.True(addresses.Single(x => x.Id == newPrimaryId).IsPrimary);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.AddressPrimaryChanged
                    && log.EntityId == newPrimaryId
                    && log.Result == AuditResults.Success);
        });
    }

    [Fact]
    public async Task SetMyPrimaryAddress_ForAddressOwnedByAnotherEmployee_ReturnsForbidden()
    {
        await _factory.ResetDatabaseAsync();

        var ownEmployeeId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();
        var otherAddressId = Guid.NewGuid();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var basicRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Basic);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "basic.forbiddenprimary@example.com",
                RoleId = basicRole.Id,
                EmployeeId = ownEmployeeId,
                IsActive = true
            };

            user.PasswordHash = passwordHashingService.HashPassword(user, "P@ssw0rd123!");

            dbContext.Users.Add(user);
            dbContext.Employees.AddRange(
                new Employee
                {
                    Id = ownEmployeeId,
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
                    Id = otherEmployeeId,
                    FirstName = "Bea",
                    LastName = "Stone",
                    Email = "bea.stone@company.com",
                    Phone = "5552223333",
                    DateOfBirth = new DateOnly(1991, 2, 10),
                    HireDate = new DateOnly(2024, 4, 1),
                    Status = EmployeeStatusCodes.Active,
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1)
                });

            dbContext.EmployeeAddresses.Add(new EmployeeAddress
            {
                Id = otherAddressId,
                EmployeeId = otherEmployeeId,
                AddressType = AddressTypeCodes.Home,
                IsPrimary = true,
                Line1 = "999 Other St",
                City = "Pittsburgh",
                State = "PA",
                ZipCode = "15211",
                CountryCode = "US",
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(2)
            });

            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "basic.forbiddenprimary@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/me/addresses/{otherAddressId}/primary");
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(403, problem!.Status);
    }

    [Fact]
    public async Task SetMyPrimaryAddress_ForDeletedOwnAddress_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        var addressId = Guid.NewGuid();

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var basicRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Basic);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "basic.deletedprimary@example.com",
                RoleId = basicRole.Id,
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

            dbContext.EmployeeAddresses.Add(new EmployeeAddress
            {
                Id = addressId,
                EmployeeId = employeeId,
                AddressType = AddressTypeCodes.Home,
                IsPrimary = false,
                Line1 = "100 Main St",
                City = "Pittsburgh",
                State = "PA",
                ZipCode = "15222",
                CountryCode = "US",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                DeletedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1)
            });

            await dbContext.SaveChangesAsync();
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "basic.deletedprimary@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/me/addresses/{addressId}/primary");
        await AntiforgeryTestHelper.AttachAsync(client, authCookie, request);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
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
