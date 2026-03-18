using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Addresses;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Domain.Auditing;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class AddressCreateEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public AddressCreateEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateEmployeeAddress_CreatesAddressAndReturnsCreated()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        await SeedEmployeeAsync(employeeId);

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/employees/{employeeId}/addresses")
        {
            Content = JsonContent.Create(new UpsertAddressRequest(
                "Home",
                true,
                "100 Main St",
                null,
                "Pittsburgh",
                "PA",
                "15222",
                "US"))
        };
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AddressWriteResponse>();
        Assert.NotNull(payload);
        Assert.Equal(employeeId, payload!.EmployeeId);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var address = await dbContext.EmployeeAddresses.SingleAsync(x => x.Id == payload.Id);

            Assert.Equal(employeeId, address.EmployeeId);
            Assert.True(address.IsPrimary);
            Assert.Equal("US", address.CountryCode);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.AddressCreated
                    && log.EntityId == address.Id
                    && log.Result == AuditResults.Success);
        });
    }

    [Fact]
    public async Task CreateEmployeeAddress_WhenPrimary_ReplacesExistingPrimary()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        var existingPrimaryId = Guid.NewGuid();

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
            });

            await Task.CompletedTask;
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/employees/{employeeId}/addresses")
        {
            Content = JsonContent.Create(new UpsertAddressRequest(
                "Mailing",
                true,
                "200 Oak Ave",
                "Apt 4",
                "Pittsburgh",
                "PA",
                "15213",
                "US"))
        };
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AddressWriteResponse>();
        Assert.NotNull(payload);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var addresses = await dbContext.EmployeeAddresses
                .Where(x => x.EmployeeId == employeeId && x.DeletedAtUtc == null)
                .OrderBy(x => x.CreatedAtUtc)
                .ToListAsync();

            Assert.Equal(2, addresses.Count);
            Assert.False(addresses.Single(x => x.Id == existingPrimaryId).IsPrimary);
            Assert.True(addresses.Single(x => x.Id == payload!.Id).IsPrimary);
        });
    }

    [Fact]
    public async Task CreateEmployeeAddress_ForDeletedEmployee_ReturnsNotFound()
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
                CreatedAtUtc = DateTimeOffset.UtcNow,
                DeletedAtUtc = DateTimeOffset.UtcNow
            });

            await Task.CompletedTask;
        });

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/employees/{employeeId}/addresses")
        {
            Content = JsonContent.Create(new UpsertAddressRequest(
                "Home",
                true,
                "100 Main St",
                null,
                "Pittsburgh",
                "PA",
                "15222",
                "US"))
        };
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

    private async Task SeedEmployeeAsync(Guid employeeId)
    {
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
    }

    private static string ExtractCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(AuthCookieName + "=", StringComparison.OrdinalIgnoreCase));

        return header.Split(';', 2, StringSplitOptions.TrimEntries)[0];
    }
}
