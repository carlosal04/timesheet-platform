using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class AddressSetPrimaryEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public AddressSetPrimaryEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SetPrimaryEmployeeAddress_ReplacesExistingPrimary()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        var existingPrimaryId = Guid.NewGuid();
        var addressId = Guid.NewGuid();

        await _factory.SeedAsync(async dbContext =>
        {
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
                    Id = addressId,
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

            await Task.CompletedTask;
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/employees/{employeeId}/addresses/{addressId}/primary");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var addresses = await dbContext.EmployeeAddresses
                .Where(x => x.EmployeeId == employeeId && x.DeletedAtUtc == null)
                .ToListAsync();

            Assert.False(addresses.Single(x => x.Id == existingPrimaryId).IsPrimary);
            Assert.True(addresses.Single(x => x.Id == addressId).IsPrimary);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.AddressPrimaryChanged
                    && log.EntityId == addressId
                    && log.Result == AuditResults.Success);
        });
    }

    [Fact]
    public async Task SetPrimaryEmployeeAddress_ForDeletedAddress_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        var addressId = Guid.NewGuid();

        await _factory.SeedAsync(async dbContext =>
        {
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
                IsPrimary = true,
                Line1 = "100 Main St",
                City = "Pittsburgh",
                State = "PA",
                ZipCode = "15222",
                CountryCode = "US",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                DeletedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1)
            });

            await Task.CompletedTask;
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/employees/{employeeId}/addresses/{addressId}/primary");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SetPrimaryEmployeeAddress_ForMissingAddress_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        await _factory.SeedAsync(async dbContext =>
        {
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

            await Task.CompletedTask;
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/employees/{employeeId}/addresses/{Guid.NewGuid()}/primary");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return ExtractCookie(response);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
    }

    private static string ExtractCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(AuthCookieName + "=", StringComparison.OrdinalIgnoreCase));

        return header.Split(';', 2, StringSplitOptions.TrimEntries)[0];
    }
}
