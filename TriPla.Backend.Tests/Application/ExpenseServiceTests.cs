using FluentAssertions;
using TriPla.Backend.Application.DTOs.Expenses;
using TriPla.Backend.Application.DTOs.Trips;
using TriPla.Backend.Application.Expenses;
using TriPla.Backend.Application.Trips;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Tests.Fakes;

namespace TriPla.Backend.Tests.Application;

[TestFixture]
public class ExpenseServiceTests
{
    private static async Task<Guid> SeedTripAsync(InMemoryUnitOfWork uow, Guid ownerId)
    {
        var service = new TripService(uow);
        var result = await service.CreateAsync(ownerId, new CreateTripRequest(
            "Trip", null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 10)));
        return result.Value!.Id;
    }

    [Test]
    public async Task AddToTripAsync_PersistsExpense()
    {
        var uow = new InMemoryUnitOfWork();
        var ownerId = Guid.NewGuid();
        var tripId = await SeedTripAsync(uow, ownerId);

        var service = new ExpenseService(uow);
        var result = await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "Hotel", null, 200m, "PLN", ExpenseCategory.Accommodation, new DateTime(2026, 1, 2), null));

        result.IsSuccess.Should().BeTrue();
        uow.ExpensesStore.Store.Should().HaveCount(1);
    }

    [Test]
    public async Task AddToTripAsync_FailsWhenTripMissing()
    {
        var uow = new InMemoryUnitOfWork();
        var service = new ExpenseService(uow);

        var result = await service.AddToTripAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateExpenseRequest(
            "X", null, 10m, "PLN", ExpenseCategory.Other, DateTime.UtcNow, null));

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task AddToTripAsync_FailsWhenSplitsDontSumToTotal()
    {
        var uow = new InMemoryUnitOfWork();
        var ownerId = Guid.NewGuid();
        var tripId = await SeedTripAsync(uow, ownerId);

        var service = new ExpenseService(uow);
        var result = await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "Dinner", null, 100m, "PLN", ExpenseCategory.Food, DateTime.UtcNow,
            new List<ExpenseSplitRequest>
            {
                new(Guid.NewGuid(), 40m),
                new(Guid.NewGuid(), 50m)
            }));

        result.IsSuccess.Should().BeFalse();
        uow.ExpensesStore.Store.Should().BeEmpty();
    }

    [Test]
    public async Task UpdateAsync_SucceedsWhenRequesterIsPayer()
    {
        var uow = new InMemoryUnitOfWork();
        var ownerId = Guid.NewGuid();
        var tripId = await SeedTripAsync(uow, ownerId);
        var service = new ExpenseService(uow);

        var created = await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "Hotel", null, 200m, "PLN", ExpenseCategory.Accommodation, new DateTime(2026, 1, 2), null));

        var updated = await service.UpdateAsync(created.Value!.Id, ownerId, new UpdateExpenseRequest(
            "Hotel premium", "dopłata", 300m, "PLN", ExpenseCategory.Accommodation, new DateTime(2026, 1, 2), null));

        updated.IsSuccess.Should().BeTrue();
        var stored = uow.ExpensesStore.Store.Values.Single();
        stored.Title.Should().Be("Hotel premium");
        stored.Amount.Amount.Should().Be(300m);
    }

    [Test]
    public async Task UpdateAsync_FailsForNonPayer()
    {
        var uow = new InMemoryUnitOfWork();
        var ownerId = Guid.NewGuid();
        var intruderId = Guid.NewGuid();
        var tripId = await SeedTripAsync(uow, ownerId);
        var service = new ExpenseService(uow);

        var created = await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "Hotel", null, 200m, "PLN", ExpenseCategory.Accommodation, new DateTime(2026, 1, 2), null));

        var updated = await service.UpdateAsync(created.Value!.Id, intruderId, new UpdateExpenseRequest(
            "Hijacked", null, 999m, "PLN", ExpenseCategory.Other, DateTime.UtcNow, null));

        updated.IsSuccess.Should().BeFalse();
        updated.Error.Should().Contain("payer");
        uow.ExpensesStore.Store.Values.Single().Title.Should().Be("Hotel");
    }

    [Test]
    public async Task UpdateAsync_LogsDiff()
    {
        var uow = new InMemoryUnitOfWork();
        var ownerId = Guid.NewGuid();
        var tripId = await SeedTripAsync(uow, ownerId);
        var service = new ExpenseService(uow);
        var created = await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "Hotel", null, 200m, "PLN", ExpenseCategory.Accommodation, new DateTime(2026, 1, 2), null));

        await service.UpdateAsync(created.Value!.Id, ownerId, new UpdateExpenseRequest(
            "Hotel", null, 250m, "PLN", ExpenseCategory.Accommodation, new DateTime(2026, 1, 2), null));

        var log = uow.ChangeLogStore.Store.Single(e => e.Type == "ExpenseUpdated");
        log.PayloadJson.Should().Contain("\"amount\":{\"before\":");
        log.PayloadJson.Should().NotContain("\"title\":{\"before\":");
    }

    [Test]
    public async Task DeleteAsync_FailsForNonPayer()
    {
        var uow = new InMemoryUnitOfWork();
        var ownerId = Guid.NewGuid();
        var intruderId = Guid.NewGuid();
        var tripId = await SeedTripAsync(uow, ownerId);
        var service = new ExpenseService(uow);

        var created = await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "Hotel", null, 200m, "PLN", ExpenseCategory.Accommodation, new DateTime(2026, 1, 2), null));

        var deleted = await service.DeleteAsync(created.Value!.Id, intruderId);

        deleted.IsSuccess.Should().BeFalse();
        deleted.Error.Should().Contain("payer");
        uow.ExpensesStore.Store.Should().HaveCount(1);
    }

    [Test]
    public async Task SetSettledAsync_TogglesFlag()
    {
        var uow = new InMemoryUnitOfWork();
        var ownerId = Guid.NewGuid();
        var tripId = await SeedTripAsync(uow, ownerId);
        var service = new ExpenseService(uow);

        var created = await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "Hotel", null, 200m, "PLN", ExpenseCategory.Accommodation, new DateTime(2026, 1, 2), null));

        var settled = await service.SetSettledAsync(created.Value!.Id, ownerId, true);
        settled.IsSuccess.Should().BeTrue();
        settled.Value!.IsSettled.Should().BeTrue();
        uow.ExpensesStore.Store[created.Value.Id].IsSettled.Should().BeTrue();

        var unsettled = await service.SetSettledAsync(created.Value.Id, ownerId, false);
        unsettled.Value!.IsSettled.Should().BeFalse();
    }

    [Test]
    public async Task SetSettledAsync_FailsForNonParticipant()
    {
        var uow = new InMemoryUnitOfWork();
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var tripId = await SeedTripAsync(uow, ownerId);
        var service = new ExpenseService(uow);
        var created = await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "Hotel", null, 200m, "PLN", ExpenseCategory.Accommodation, new DateTime(2026, 1, 2), null));

        var result = await service.SetSettledAsync(created.Value!.Id, outsiderId, true);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("participant");
    }

    [Test]
    public async Task SetSettledAsync_AllowsAnyParticipant()
    {
        var uow = new InMemoryUnitOfWork();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var tripId = await SeedTripAsync(uow, ownerId);
        await uow.Participants.AddAsync(new Participant(tripId, memberId, ParticipantRole.Member));
        var service = new ExpenseService(uow);
        var created = await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "Hotel", null, 200m, "PLN", ExpenseCategory.Accommodation, new DateTime(2026, 1, 2), null));

        var result = await service.SetSettledAsync(created.Value!.Id, memberId, true);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsSettled.Should().BeTrue();
    }

    [Test]
    public async Task SetAllSettledAsync_MarksEveryExpense()
    {
        var uow = new InMemoryUnitOfWork();
        var ownerId = Guid.NewGuid();
        var tripId = await SeedTripAsync(uow, ownerId);
        var service = new ExpenseService(uow);
        await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "A", null, 10m, "PLN", ExpenseCategory.Other, DateTime.UtcNow, null));
        await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "B", null, 20m, "PLN", ExpenseCategory.Other, DateTime.UtcNow, null));

        var result = await service.SetAllSettledAsync(tripId, ownerId, true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
        uow.ExpensesStore.Store.Values.Should().OnlyContain(e => e.IsSettled);
    }

    [Test]
    public async Task SetAllSettledAsync_FailsForMember()
    {
        var uow = new InMemoryUnitOfWork();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var tripId = await SeedTripAsync(uow, ownerId);
        await uow.Participants.AddAsync(new Participant(tripId, memberId, ParticipantRole.Member));
        var service = new ExpenseService(uow);
        await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "A", null, 10m, "PLN", ExpenseCategory.Other, DateTime.UtcNow, null));

        var result = await service.SetAllSettledAsync(tripId, memberId, true);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("editors");
        uow.ExpensesStore.Store.Values.Should().OnlyContain(e => !e.IsSettled);
    }

    [Test]
    public async Task SetAllSettledAsync_LogsOnlyWhenSomethingChanged()
    {
        var uow = new InMemoryUnitOfWork();
        var ownerId = Guid.NewGuid();
        var tripId = await SeedTripAsync(uow, ownerId);
        var service = new ExpenseService(uow);
        await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "A", null, 10m, "PLN", ExpenseCategory.Other, DateTime.UtcNow, null));

        await service.SetAllSettledAsync(tripId, ownerId, true);
        var firstCount = uow.ChangeLogStore.Store.Count(e => e.Type == "AllExpensesSettled");
        await service.SetAllSettledAsync(tripId, ownerId, true);
        var secondCount = uow.ChangeLogStore.Store.Count(e => e.Type == "AllExpensesSettled");

        firstCount.Should().Be(1);
        secondCount.Should().Be(1);
    }

    [Test]
    public async Task DeleteAsync_RemovesExpense()
    {
        var uow = new InMemoryUnitOfWork();
        var ownerId = Guid.NewGuid();
        var tripId = await SeedTripAsync(uow, ownerId);
        var service = new ExpenseService(uow);

        var created = await service.AddToTripAsync(tripId, ownerId, new CreateExpenseRequest(
            "Hotel", null, 200m, "PLN", ExpenseCategory.Accommodation, DateTime.UtcNow, null));

        var deleted = await service.DeleteAsync(created.Value!.Id, ownerId);

        deleted.IsSuccess.Should().BeTrue();
        uow.ExpensesStore.Store.Should().BeEmpty();
    }
}
