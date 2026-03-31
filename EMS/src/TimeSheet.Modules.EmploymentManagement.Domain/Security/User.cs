namespace TimeSheet.Modules.EmploymentManagement.Domain.Security;

public sealed class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Guid RoleId { get; set; }

    public Role? Role { get; set; }

    public Guid? EmployeeId { get; set; }

    public bool IsActive { get; set; } = true;

    public int AccessFailedCount { get; set; }

    public DateTimeOffset? LockoutEndUtc { get; set; }

    public int SessionVersion { get; set; }

    public bool MustChangePassword { get; set; }

    public DateTimeOffset? TemporaryPasswordExpiresAtUtc { get; set; }

    public DateTimeOffset? LastTemporaryPasswordIssuedAtUtc { get; set; }

    public bool IsLockedOut(DateTimeOffset nowUtc)
    {
        return LockoutEndUtc.HasValue && LockoutEndUtc.Value > nowUtc;
    }

    public void RecordFailedAccess(int threshold, TimeSpan lockoutDuration, DateTimeOffset nowUtc)
    {
        AccessFailedCount++;

        if (threshold > 0 && AccessFailedCount >= threshold)
        {
            LockoutEndUtc = nowUtc.Add(lockoutDuration);
            AccessFailedCount = 0;
        }
    }

    public void ResetFailedAccess()
    {
        AccessFailedCount = 0;
        LockoutEndUtc = null;
    }

    public int IncrementSessionVersion()
    {
        SessionVersion++;
        return SessionVersion;
    }

    public void ClearTemporaryPasswordState()
    {
        MustChangePassword = false;
        TemporaryPasswordExpiresAtUtc = null;
    }
}
