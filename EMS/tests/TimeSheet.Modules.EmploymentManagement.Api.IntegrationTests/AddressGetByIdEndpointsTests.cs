using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AddressDetail = TimeSheet.Modules.EmploymentManagement.Application.Addresses.GetById.Address;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class AddressGetByIdEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public AddressGetByIdEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEmployeeAddressById_ReturnsActiveAddress()
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
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/employees/{employeeId}/addresses/{addressId}");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AddressDetail>();
        Assert.NotNull(payload);
        Assert.Equal(addressId, payload!.Id);
        Assert.Equal(employeeId, payload.EmployeeId);
        Assert.True(payload.IsPrimary);
    }

    [Fact]
    public async Task GetEmployeeAddressById_ReturnsNotFound_ForDeletedAddress()
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

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/employees/{employeeId}/addresses/{addressId}");
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

    private static string ExtractCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(AuthCookieName + "=", StringComparison.OrdinalIgnoreCase));

        return header.Split(';', 2, StringSplitOptions.TrimEntries)[0];
    }
}
