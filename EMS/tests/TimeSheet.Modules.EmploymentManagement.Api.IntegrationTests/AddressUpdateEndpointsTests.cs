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

public sealed class AddressUpdateEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public AddressUpdateEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateEmployeeAddress_UpdatesFieldsAndReturnsOk()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        await SeedEmployeeWithAddressAsync(employeeId, addressId, isPrimary: false);

        using var client = CreateClient();
        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/employees/{employeeId}/addresses/{addressId}")
        {
            Content = JsonContent.Create(new UpsertAddressRequest(
                "Mailing",
                false,
                "200 Oak Ave",
                "Apt 4",
                "Pittsburgh",
                "PA",
                "15213",
                "US"))
        };
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AddressWriteResponse>();
        Assert.NotNull(payload);
        Assert.Equal(employeeId, payload!.EmployeeId);
        Assert.Equal(addressId, payload.Id);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var address = await dbContext.EmployeeAddresses.SingleAsync(x => x.Id == addressId);

            Assert.Equal("Mailing", address.AddressType);
            Assert.Equal("200 Oak Ave", address.Line1);
            Assert.Equal("Apt 4", address.Line2);
            Assert.False(address.IsPrimary);
            Assert.Contains(
                dbContext.AuditLogs,
                log => log.ActionType == AuditActionTypes.AddressUpdated
                    && log.EntityId == addressId
                    && log.Result == AuditResults.Success);
        });
    }

    [Fact]
    public async Task UpdateEmployeeAddress_WhenSetPrimary_ReplacesExistingPrimary()
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
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/employees/{employeeId}/addresses/{addressId}")
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

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var addresses = await dbContext.EmployeeAddresses
                .Where(x => x.EmployeeId == employeeId && x.DeletedAtUtc == null)
                .OrderBy(x => x.CreatedAtUtc)
                .ToListAsync();

            Assert.Equal(2, addresses.Count);
            Assert.False(addresses.Single(x => x.Id == existingPrimaryId).IsPrimary);
            Assert.True(addresses.Single(x => x.Id == addressId).IsPrimary);
        });
    }

    [Fact]
    public async Task UpdateEmployeeAddress_ForDeletedAddress_ReturnsNotFound()
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
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/employees/{employeeId}/addresses/{addressId}")
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

    private async Task SeedEmployeeWithAddressAsync(Guid employeeId, Guid addressId, bool isPrimary)
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

            dbContext.EmployeeAddresses.Add(new EmployeeAddress
            {
                Id = addressId,
                EmployeeId = employeeId,
                AddressType = AddressTypeCodes.Home,
                IsPrimary = isPrimary,
                Line1 = "100 Main St",
                City = "Pittsburgh",
                State = "PA",
                ZipCode = "15222",
                CountryCode = "US",
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
