using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AddressListResult = TimeSheet.Modules.EmploymentManagement.Application.Addresses.List.Result;
using TimeSheet.Modules.EmploymentManagement.Api.Contracts.Auth;
using TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Security;
using TimeSheet.Modules.EmploymentManagement.Domain.Employees;
using TimeSheet.Modules.EmploymentManagement.Domain.Security;
using TimeSheet.Modules.EmploymentManagement.Infrastructure.Persistence;

namespace TimeSheet.Modules.EmploymentManagement.Api.IntegrationTests;

public sealed class AddressListEndpointsTests : IClassFixture<AuthApiFactory>
{
    private const string AuthCookieName = "ems.auth";
    private readonly AuthApiFactory _factory;

    public AddressListEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEmployeeAddresses_ReturnsPrimaryFirstAndHidesDeletedByDefault()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        var primaryAddressId = Guid.NewGuid();
        var secondaryAddressId = Guid.NewGuid();

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

            await Task.CompletedTask;
        });

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var authCookie = await LoginAsync(client, "admin@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/employees/{employeeId}/addresses");
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
    public async Task GetEmployeeAddresses_IncludeDeletedIsForbiddenForBasic()
    {
        await _factory.ResetDatabaseAsync();

        var employeeId = Guid.NewGuid();
        await _factory.ExecuteScopedAsync(async services =>
        {
            var dbContext = services.GetRequiredService<EmploymentManagementDbContext>();
            var passwordHashingService = services.GetRequiredService<IPasswordHashingService>();
            var basicRole = await dbContext.Roles.SingleAsync(x => x.Code == RoleCodes.Basic);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "basic.address.reader@example.com",
                RoleId = basicRole.Id,
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

            await dbContext.SaveChangesAsync();
        });

        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var authCookie = await LoginAsync(client, "basic.address.reader@example.com", "P@ssw0rd123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/employees/{employeeId}/addresses?includeDeleted=true");
        request.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"Expected {(int)HttpStatusCode.OK} OK but received {(int)response.StatusCode} {response.StatusCode}.{Environment.NewLine}{body}");
        }

        return ExtractCookie(response);
    }

    private static string ExtractCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(AuthCookieName + "=", StringComparison.OrdinalIgnoreCase));

        return header.Split(';', 2, StringSplitOptions.TrimEntries)[0];
    }
}
