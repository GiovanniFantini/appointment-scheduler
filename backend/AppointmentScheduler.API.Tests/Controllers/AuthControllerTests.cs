using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IPasswordResetService> _passwordResetService = new();

    [Fact]
    public async Task MerchantLogin_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        var request = new LoginRequest { Email = "merchant@example.com", Password = "secret" };
        _authService.Setup(service => service.LoginMerchantAsync(request)).ReturnsAsync((AuthResponse?)null);
        var controller = CreateController();

        var result = await controller.MerchantLogin(request);

        var unauthorized = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorized.GetAnonymousString("message").Should().Be("Email o password non validi");
    }

    [Fact]
    public async Task MerchantLogin_ReturnsOk_WhenCredentialsAreValid()
    {
        var request = new LoginRequest { Email = "merchant@example.com", Password = "secret" };
        var response = new AuthResponse { Token = "jwt" };
        _authService.Setup(service => service.LoginMerchantAsync(request)).ReturnsAsync(response);
        var controller = CreateController();

        var result = await controller.MerchantLogin(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task MerchantRegister_ReturnsOkWithGenericMessage_WhenEmailIsAlreadyRegistered()
    {
        // Anti-enumeration: il controller risponde 200 OK con messaggio generico
        // sia se l'email è libera sia se è già usata. AuthService notifica
        // l'utente legittimo per email (best-effort, non bloccante).
        var request = new RegisterMerchantRequest { Email = "merchant@example.com", Password = "Secret12345!", CompanyName = "Contoso" };
        _authService.Setup(service => service.RegisterMerchantAsync(request)).ReturnsAsync((AuthResponse?)null);
        var controller = CreateController();

        var result = await controller.MerchantRegister(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.GetAnonymousString("message").Should().Be(
            "Se i dati sono validi, riceverai a breve un'email di conferma.");
    }

    [Fact]
    public async Task MerchantRegister_ReturnsBadRequest_WhenServiceThrowsArgumentException()
    {
        var request = new RegisterMerchantRequest { Email = "merchant@example.com", Password = "short", CompanyName = "Contoso" };
        _authService.Setup(service => service.RegisterMerchantAsync(request)).ThrowsAsync(new ArgumentException("Password troppo corta"));
        var controller = CreateController();

        var result = await controller.MerchantRegister(request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Password troppo corta");
    }

    [Fact]
    public async Task MerchantRegister_ReturnsOk_WhenRegistrationSucceeds()
    {
        var request = new RegisterMerchantRequest { Email = "merchant@example.com", Password = "secret123", CompanyName = "Contoso" };
        var response = new AuthResponse { Token = "jwt" };
        _authService.Setup(service => service.RegisterMerchantAsync(request)).ReturnsAsync(response);
        var controller = CreateController();

        var result = await controller.MerchantRegister(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task AdminLogin_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        var request = new LoginRequest { Email = "admin@example.com", Password = "secret" };
        _authService.Setup(service => service.LoginAdminAsync(request)).ReturnsAsync((AuthResponse?)null);
        var controller = CreateController();

        var result = await controller.AdminLogin(request);

        var unauthorized = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorized.GetAnonymousString("message").Should().Be("Email o password non validi");
    }

    [Fact]
    public async Task AdminLogin_ReturnsOk_WhenCredentialsAreValid()
    {
        var request = new LoginRequest { Email = "admin@example.com", Password = "secret" };
        var response = new AuthResponse { Token = "jwt" };
        _authService.Setup(service => service.LoginAdminAsync(request)).ReturnsAsync(response);
        var controller = CreateController();

        var result = await controller.AdminLogin(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task EmployeeLogin_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        var request = new LoginRequest { Email = "employee@example.com", Password = "secret" };
        _authService.Setup(service => service.LoginEmployeeAsync(request)).ReturnsAsync((AuthResponse?)null);
        var controller = CreateController();

        var result = await controller.EmployeeLogin(request);

        var unauthorized = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorized.GetAnonymousString("message").Should().Be("Email o password non validi");
    }

    [Fact]
    public async Task EmployeeLogin_ReturnsOk_WhenCredentialsAreValid()
    {
        var request = new LoginRequest { Email = "employee@example.com", Password = "secret" };
        var response = new AuthResponse { Token = "jwt" };
        _authService.Setup(service => service.LoginEmployeeAsync(request)).ReturnsAsync(response);
        var controller = CreateController();

        var result = await controller.EmployeeLogin(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task EmployeeRegister_ReturnsOkWithGenericMessage_WhenEmailIsAlreadyRegistered()
    {
        var request = new EmployeeRegisterRequest { Email = "employee@example.com", Password = "Secret12345!", FirstName = "Jane", LastName = "Doe" };
        _authService.Setup(service => service.RegisterEmployeeAsync(request)).ReturnsAsync((AuthResponse?)null);
        var controller = CreateController();

        var result = await controller.EmployeeRegister(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.GetAnonymousString("message").Should().Be(
            "Se i dati sono validi, riceverai a breve un'email di conferma.");
    }

    [Fact]
    public async Task EmployeeRegister_ReturnsBadRequest_WhenServiceThrowsArgumentException()
    {
        var request = new EmployeeRegisterRequest { Email = "employee@example.com", Password = "short", FirstName = "Jane", LastName = "Doe" };
        _authService.Setup(service => service.RegisterEmployeeAsync(request)).ThrowsAsync(new ArgumentException("Password troppo corta"));
        var controller = CreateController();

        var result = await controller.EmployeeRegister(request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Password troppo corta");
    }

    [Fact]
    public async Task EmployeeRegister_ReturnsOk_WhenRegistrationSucceeds()
    {
        var request = new EmployeeRegisterRequest { Email = "employee@example.com", Password = "secret123", FirstName = "Jane", LastName = "Doe" };
        var response = new AuthResponse { Token = "jwt" };
        _authService.Setup(service => service.RegisterEmployeeAsync(request)).ReturnsAsync(response);
        var controller = CreateController();

        var result = await controller.EmployeeRegister(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task SelectCompany_ReturnsUnauthorized_WhenUserClaimIsMissing()
    {
        var controller = CreateController();

        var result = await controller.SelectCompany(7);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        _authService.Verify(service => service.SelectCompanyAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SelectCompany_ReturnsForbid_WhenCompanyIsNotAvailable()
    {
        _authService.Setup(service => service.SelectCompanyAsync(12, 7)).ReturnsAsync((AuthResponse?)null);
        var controller = CreateController(new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.SelectCompany(7);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SelectCompany_ReturnsOk_WhenCompanySelectionSucceeds()
    {
        var response = new AuthResponse { Token = "jwt" };
        _authService.Setup(service => service.SelectCompanyAsync(12, 7)).ReturnsAsync(response);
        var controller = CreateController(new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.SelectCompany(7);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task ForgotPassword_ReturnsOk_AndAlwaysUsesGenericMessage()
    {
        var request = new ForgotPasswordRequest { Email = "person@example.com" };
        var controller = CreateController();

        var result = await controller.ForgotPassword(request);

        result.Should().BeOfType<OkObjectResult>();
        result.GetAnonymousString("message").Should().Be("Se l'email risulta registrata, riceverai le istruzioni per il recupero della password.");
        _passwordResetService.Verify(service => service.RequestPasswordResetAsync("person@example.com"), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ReturnsBadRequest_WhenResetFails()
    {
        var request = new ResetPasswordRequest { Token = "expired", NewPassword = "secret123" };
        _passwordResetService.Setup(service => service.ResetPasswordAsync("expired", "secret123")).ReturnsAsync(false);
        var controller = CreateController();

        var result = await controller.ResetPassword(request);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Il link non è più valido. Richiedi un nuovo recupero password.");
    }

    [Fact]
    public async Task ResetPassword_ReturnsOk_WhenResetSucceeds()
    {
        var request = new ResetPasswordRequest { Token = "valid", NewPassword = "secret123" };
        _passwordResetService.Setup(service => service.ResetPasswordAsync("valid", "secret123")).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.ResetPassword(request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.GetAnonymousString("message").Should().Be("Password aggiornata con successo.");
    }

    private AuthController CreateController(params Claim[] claims)
    {
        return new AuthController(_authService.Object, _passwordResetService.Object).WithUser(claims);
    }
}