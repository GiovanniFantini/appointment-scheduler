using AppointmentScheduler.API.Tests.Helpers;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.API.Tests;

public class PasswordResetServiceTests
{
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IPasswordResetTokenGenerator> _tokenGenerator = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IUtcClock> _clock = new();
    private readonly Mock<ILogger<PasswordResetService>> _logger = new();
    private readonly FrontendUrlOptions _frontendUrls = new() { Employee = "https://employee.example.test" };

    [Fact]
    public async Task RequestPasswordResetAsync_ReturnsTrue_WhenUserDoesNotExist()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.Users, user => [user.Id])
            .WithEmptySet(x => x.PasswordResetTokens, token => [token.Id])
            .Build();
        var service = CreateService(context.Object);

        var result = await service.RequestPasswordResetAsync("missing@example.com");

        result.Should().BeTrue();
        _emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_ReturnsTrue_WhenRateLimited()
    {
        var now = new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc);
        var users = new List<User>
        {
            new() { Id = 7, Email = "user@example.com", FirstName = "Eva", LastName = "User", AccountType = AccountType.Employee, IsActive = true }
        };
        var tokens = new List<PasswordResetToken>
        {
            new() { Id = 1, UserId = 7, Token = "recent", CreatedAt = now.AddSeconds(-30), ExpiresAt = now.AddHours(1) }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, user => [user.Id])
            .WithSet(x => x.PasswordResetTokens, tokens, token => [token.Id])
            .Build();
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        var service = CreateService(context.Object);

        var result = await service.RequestPasswordResetAsync("user@example.com");

        result.Should().BeTrue();
        tokens.Should().HaveCount(1);
        _emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_InvalidatesPendingTokensAndCreatesNewOne()
    {
        var now = new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc);
        var users = new List<User>
        {
            new() { Id = 7, Email = "user@example.com", FirstName = "Eva", LastName = "User", AccountType = AccountType.Employee, IsActive = true }
        };
        var tokens = new List<PasswordResetToken>
        {
            new() { Id = 1, UserId = 7, Token = "old", CreatedAt = now.AddHours(-2), ExpiresAt = now.AddHours(-1) },
            new() { Id = 2, UserId = 7, Token = "pending", CreatedAt = now.AddMinutes(-65), ExpiresAt = now.AddMinutes(-5) }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, users, user => [user.Id])
            .WithSet(x => x.PasswordResetTokens, tokens, token => [token.Id])
            .Build(out var saveChangesTracker);
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _tokenGenerator.Setup(x => x.Generate()).Returns("fresh-token");
        var service = CreateService(context.Object);

        var result = await service.RequestPasswordResetAsync(" user@example.com ");

        result.Should().BeTrue();
        tokens.Should().ContainSingle(token => token.Token == "fresh-token");
        tokens.Single(token => token.Token == "pending").UsedAt.Should().Be(now);
        tokens.Single(token => token.Token == "fresh-token").ExpiresAt.Should().Be(now.AddHours(1));
        saveChangesTracker.Count.Should().Be(1);
        _emailService.Verify(x => x.SendAsync(
            "user@example.com",
            "Eva User",
            "Recupero password",
            It.Is<string>(body => body.Contains("fresh-token") && body.Contains("https://employee.example.test/reset-password"))), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsFalse_WhenPasswordIsTooShort()
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.Users, user => [user.Id])
            .WithEmptySet(x => x.PasswordResetTokens, token => [token.Id])
            .Build();
        var service = CreateService(context.Object);

        var result = await service.ResetPasswordAsync("token", "short");

        result.Should().Be(ResetPasswordResult.InvalidInput);
        _passwordHasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("alllowercase12")]   // ≥12 ma senza maiuscola/cifra
    [InlineData("ALLUPPERCASE12")]   // manca minuscola
    [InlineData("NoDigitsAtAllAB")]  // manca cifra
    public async Task ResetPasswordAsync_ReturnsInvalidInput_WhenComplexityMissing(string password)
    {
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.Users, user => [user.Id])
            .WithEmptySet(x => x.PasswordResetTokens, token => [token.Id])
            .Build();
        var service = CreateService(context.Object);

        var result = await service.ResetPasswordAsync("token", password);

        result.Should().Be(ResetPasswordResult.InvalidInput);
        _passwordHasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_UpdatesHashAndMarksTokenUsed_WhenTokenIsValid()
    {
        var now = new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc);
        var user = new User { Id = 7, Email = "user@example.com", PasswordHash = "old-hash", AccountType = AccountType.Employee, IsActive = true };
        var tokens = new List<PasswordResetToken>
        {
            new() { Id = 1, UserId = 7, Token = "valid-token", CreatedAt = now.AddMinutes(-5), ExpiresAt = now.AddMinutes(30), User = user }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User> { user }, entity => [entity.Id])
            .WithSet(x => x.PasswordResetTokens, tokens, token => [token.Id])
            .Build(out var saveChangesTracker);
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _passwordHasher.Setup(x => x.HashPassword("New-Password-123")).Returns("new-hash");
        var service = CreateService(context.Object);

        var result = await service.ResetPasswordAsync("valid-token", "New-Password-123");

        result.Should().Be(ResetPasswordResult.Success);
        user.PasswordHash.Should().Be("new-hash");
        user.UpdatedAt.Should().Be(now);
        tokens[0].UsedAt.Should().Be(now);
        saveChangesTracker.Count.Should().Be(1);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsTokenNotFound_WhenTokenDoesNotExist()
    {
        var now = new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc);
        var context = new ApplicationDbContextMockBuilder()
            .WithEmptySet(x => x.Users, user => [user.Id])
            .WithEmptySet(x => x.PasswordResetTokens, token => [token.Id])
            .Build();
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        var service = CreateService(context.Object);

        var result = await service.ResetPasswordAsync("missing-token", "New-Password-123");

        result.Should().Be(ResetPasswordResult.TokenNotFound);
        _passwordHasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsTokenExpired_WhenTokenIsExpired()
    {
        var now = new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc);
        var user = new User { Id = 7, Email = "user@example.com", PasswordHash = "old-hash", AccountType = AccountType.Employee, IsActive = true };
        var tokens = new List<PasswordResetToken>
        {
            new() { Id = 1, UserId = 7, Token = "expired-token", CreatedAt = now.AddHours(-2), ExpiresAt = now.AddMinutes(-1), User = user }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User> { user }, entity => [entity.Id])
            .WithSet(x => x.PasswordResetTokens, tokens, token => [token.Id])
            .Build();
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        var service = CreateService(context.Object);

        var result = await service.ResetPasswordAsync("expired-token", "New-Password-123");

        result.Should().Be(ResetPasswordResult.TokenExpired);
        user.PasswordHash.Should().Be("old-hash");
        _passwordHasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsTokenAlreadyUsed_WhenTokenWasUsed()
    {
        var now = new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc);
        var user = new User { Id = 7, Email = "user@example.com", PasswordHash = "old-hash", AccountType = AccountType.Employee, IsActive = true };
        var tokens = new List<PasswordResetToken>
        {
            new() { Id = 1, UserId = 7, Token = "used-token", CreatedAt = now.AddMinutes(-5), ExpiresAt = now.AddMinutes(30), UsedAt = now.AddMinutes(-2), User = user }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User> { user }, entity => [entity.Id])
            .WithSet(x => x.PasswordResetTokens, tokens, token => [token.Id])
            .Build();
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        var service = CreateService(context.Object);

        var result = await service.ResetPasswordAsync("used-token", "New-Password-123");

        result.Should().Be(ResetPasswordResult.TokenAlreadyUsed);
        user.PasswordHash.Should().Be("old-hash");
        _passwordHasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_AcceptsLegacyToken_WhenPlusIsConvertedToSpace()
    {
        var now = new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc);
        var user = new User { Id = 11, Email = "legacy@example.com", PasswordHash = "old-hash", AccountType = AccountType.Employee, IsActive = true };
        var tokens = new List<PasswordResetToken>
        {
            new() { Id = 9, UserId = 11, Token = "abc+def/ghi==", CreatedAt = now.AddMinutes(-5), ExpiresAt = now.AddMinutes(30), User = user }
        };
        var context = new ApplicationDbContextMockBuilder()
            .WithSet(x => x.Users, new List<User> { user }, entity => [entity.Id])
            .WithSet(x => x.PasswordResetTokens, tokens, token => [token.Id])
            .Build(out var saveChangesTracker);
        _clock.SetupGet(x => x.UtcNow).Returns(now);
        _passwordHasher.Setup(x => x.HashPassword("New-Password-123")).Returns("new-hash");
        var service = CreateService(context.Object);

        var result = await service.ResetPasswordAsync("abc def/ghi==", "New-Password-123");

        result.Should().Be(ResetPasswordResult.Success);
        user.PasswordHash.Should().Be("new-hash");
        tokens[0].UsedAt.Should().Be(now);
        saveChangesTracker.Count.Should().Be(1);
    }

    private PasswordResetService CreateService(IApplicationDbContext context)
    {
        _clock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc));
        return new PasswordResetService(
            context,
            _emailService.Object,
            _frontendUrls,
            _tokenGenerator.Object,
            _passwordHasher.Object,
            _clock.Object,
            _logger.Object);
    }
}