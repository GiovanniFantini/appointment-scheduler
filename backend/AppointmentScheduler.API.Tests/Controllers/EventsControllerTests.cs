using AppointmentScheduler.API.Tests.Helpers;

namespace AppointmentScheduler.API.Tests.Controllers;

public class EventsControllerTests
{
    private readonly Mock<IEventService> _eventService = new();

    [Fact]
    public async Task GetMerchantEvents_ReturnsBadRequest_WhenMerchantClaimIsMissingForNonAdmin()
    {
        var controller = CreateController();

        var result = await controller.GetMerchantEvents(null, null, null);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Merchant ID non trovato nel token");
    }

    [Fact]
    public async Task GetMerchantEvents_UsesQueryMerchantId_ForAdmin()
    {
        var events = new List<EventDto> { new() { Id = 1 } };
        _eventService.Setup(service => service.GetMerchantEventsAsync(9, null, null, EventType.Turno, 2, 3)).ReturnsAsync(events);
        var controller = CreateController(new Claim(ClaimTypes.Role, "Admin"));

        var result = await controller.GetMerchantEvents(null, null, EventType.Turno, 2, 3, 9);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(events);
    }

    [Fact]
    public async Task GetEmployeeEvents_ReturnsBadRequest_WhenEmployeeClaimIsMissing()
    {
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetEmployeeEvents(null, null);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Employee ID non trovato nel token");
    }

    [Fact]
    public async Task GetEmployeeEvents_ReturnsOk_WhenClaimsAreValid()
    {
        var events = new List<EventDto> { new() { Id = 1 } };
        _eventService.Setup(service => service.GetEmployeeEventsAsync(11, 7, null, null)).ReturnsAsync(events);
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"));

        var result = await controller.GetEmployeeEvents(null, null);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(events);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenEventDoesNotExist()
    {
        _eventService.Setup(service => service.GetByIdAsync(5, 7)).ReturnsAsync((EventDto?)null);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetById(5);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Evento non trovato");
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenEventExists()
    {
        var evt = new EventDto { Id = 5 };
        _eventService.Setup(service => service.GetByIdAsync(5, 7)).ReturnsAsync(evt);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetById(5);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(evt);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenUserClaimIsMissing()
    {
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Create(new CreateEventRequest());

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("User ID non trovato nel token");
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenServiceThrows()
    {
        var request = new CreateEventRequest();
        _eventService.Setup(service => service.CreateAsync(7, 12, request)).ThrowsAsync(new Exception("boom"));
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.Create(request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Errore nella creazione dell'evento: boom");
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenServiceSucceeds()
    {
        var request = new CreateEventRequest();
        var evt = new EventDto { Id = 9 };
        _eventService.Setup(service => service.CreateAsync(7, 12, request)).ReturnsAsync(evt);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.Create(request);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(EventsController.GetById));
        created.RouteValues!["id"].Should().Be(9);
        created.Value.Should().BeSameAs(evt);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        var request = new UpdateEventRequest();
        _eventService.Setup(service => service.UpdateAsync(5, 7, request)).ThrowsAsync(new InvalidOperationException("Filiale non valida"));
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Update(5, request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Filiale non valida");
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenEventDoesNotExist()
    {
        var request = new UpdateEventRequest();
        _eventService.Setup(service => service.UpdateAsync(5, 7, request)).ReturnsAsync((EventDto?)null);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Update(5, request);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Evento non trovato o non autorizzato");
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenEventIsUpdated()
    {
        var request = new UpdateEventRequest();
        var evt = new EventDto { Id = 5 };
        _eventService.Setup(service => service.UpdateAsync(5, 7, request)).ReturnsAsync(evt);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Update(5, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(evt);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenEventDoesNotExist()
    {
        _eventService.Setup(service => service.DeleteAsync(5, 7)).ReturnsAsync(false);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Delete(5);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Evento non trovato o non autorizzato");
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenEventIsDeleted()
    {
        _eventService.Setup(service => service.DeleteAsync(5, 7)).ReturnsAsync(true);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Delete(5);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.GetAnonymousString("message").Should().Be("Evento eliminato con successo");
    }

    [Fact]
    public async Task GetEmployeeEffectiveSchedule_ReturnsBadRequest_WhenFromIsAfterTo()
    {
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"));

        var result = await controller.GetEmployeeEffectiveSchedule(new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 1));

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Data di inizio successiva alla data di fine");
    }

    [Fact]
    public async Task GetEmployeeEffectiveSchedule_ReturnsOk_WhenRangeIsValid()
    {
        var from = new DateOnly(2026, 2, 1);
        var to = new DateOnly(2026, 2, 2);
        var schedule = new List<EffectiveShiftDto> { new() { EmployeeId = 11 } };
        _eventService.Setup(service => service.GetEffectiveScheduleAsync(11, 7, from, to)).ReturnsAsync(schedule);
        var controller = CreateController(new Claim("EmployeeId", "11"), new Claim("MerchantId", "7"));

        var result = await controller.GetEmployeeEffectiveSchedule(from, to);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(schedule);
    }

    [Fact]
    public async Task GetEffectiveScheduleForEmployee_ReturnsBadRequest_WhenFromIsAfterTo()
    {
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetEffectiveScheduleForEmployee(11, new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 1));

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Data di inizio successiva alla data di fine");
    }

    [Fact]
    public async Task GetEffectiveScheduleForEmployee_ReturnsOk_WhenRangeIsValid()
    {
        var from = new DateOnly(2026, 2, 1);
        var to = new DateOnly(2026, 2, 2);
        var schedule = new List<EffectiveShiftDto> { new() { EmployeeId = 11 } };
        _eventService.Setup(service => service.GetEffectiveScheduleAsync(11, 7, from, to)).ReturnsAsync(schedule);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.GetEffectiveScheduleForEmployee(11, from, to);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(schedule);
    }

    [Fact]
    public async Task Clone_ReturnsBadRequest_WhenRangeIsInvalid()
    {
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Clone(5, new CloneEventRequest { FromDate = new DateOnly(2026, 2, 2), ToDate = new DateOnly(2026, 2, 1) });

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("La data di inizio deve essere precedente o uguale alla data di fine");
    }

    [Fact]
    public async Task Clone_ReturnsNotFound_WhenOriginalEventDoesNotExist()
    {
        var request = new CloneEventRequest { FromDate = new DateOnly(2026, 2, 1), ToDate = new DateOnly(2026, 2, 2) };
        _eventService.Setup(service => service.CloneAsync(5, 7, request)).ReturnsAsync([]);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Clone(5, request);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.GetAnonymousString("message").Should().Be("Evento originale non trovato o non autorizzato");
    }

    [Fact]
    public async Task Clone_ReturnsBadRequest_WhenServiceThrows()
    {
        var request = new CloneEventRequest { FromDate = new DateOnly(2026, 2, 1), ToDate = new DateOnly(2026, 2, 2) };
        _eventService.Setup(service => service.CloneAsync(5, 7, request)).ThrowsAsync(new Exception("boom"));
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Clone(5, request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Errore nella clonazione dell'evento: boom");
    }

    [Fact]
    public async Task Clone_ReturnsOk_WhenServiceSucceeds()
    {
        var request = new CloneEventRequest { FromDate = new DateOnly(2026, 2, 1), ToDate = new DateOnly(2026, 2, 2) };
        var cloned = new List<EventDto> { new() { Id = 10 } };
        _eventService.Setup(service => service.CloneAsync(5, 7, request)).ReturnsAsync(cloned);
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.Clone(5, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(cloned);
    }

    [Fact]
    public async Task CloneWeek_ReturnsBadRequest_WhenUserClaimIsMissing()
    {
        var controller = CreateController(new Claim("MerchantId", "7"));

        var result = await controller.CloneWeek(new CloneWeekRequest { NumberOfWeeks = 1 });

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("User ID non trovato nel token");
    }

    [Fact]
    public async Task CloneWeek_ReturnsBadRequest_WhenNumberOfWeeksIsBelowMinimum()
    {
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.CloneWeek(new CloneWeekRequest { NumberOfWeeks = 0 });

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Il numero di settimane deve essere almeno 1");
    }

    [Fact]
    public async Task CloneWeek_ReturnsBadRequest_WhenNumberOfWeeksExceedsMaximum()
    {
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.CloneWeek(new CloneWeekRequest { NumberOfWeeks = 53 });

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Il numero massimo di settimane è 52");
    }

    [Fact]
    public async Task CloneWeek_ReturnsBadRequest_WhenServiceThrows()
    {
        var request = new CloneWeekRequest { NumberOfWeeks = 1 };
        _eventService.Setup(service => service.CloneWeekAsync(7, 12, request)).ThrowsAsync(new Exception("boom"));
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.CloneWeek(request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.GetAnonymousString("message").Should().Be("Errore nella clonazione settimanale: boom");
    }

    [Fact]
    public async Task CloneWeek_ReturnsOk_WhenServiceSucceeds()
    {
        var request = new CloneWeekRequest { NumberOfWeeks = 1 };
        var cloned = new List<EventDto> { new() { Id = 15 } };
        _eventService.Setup(service => service.CloneWeekAsync(7, 12, request)).ReturnsAsync(cloned);
        var controller = CreateController(new Claim("MerchantId", "7"), new Claim(ClaimTypes.NameIdentifier, "12"));

        var result = await controller.CloneWeek(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(cloned);
    }

    private EventsController CreateController(params Claim[] claims)
    {
        return new EventsController(_eventService.Object).WithUser(claims);
    }
}