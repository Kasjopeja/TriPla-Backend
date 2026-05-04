using Microsoft.Extensions.Logging;
using TriPla.Backend.Application.Common;
using TriPla.Backend.Application.Interfaces;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;
using TriPla.Backend.Domain.ValueObjects;

namespace TriPla.Backend.Infrastructure.Persistence;

public class DataSeeder
{
    private const string DefaultPassword = "Password123!";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ILogger<DataSeeder> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _unitOfWork.Users.ExistsByEmailAsync("alice@example.com", ct))
        {
            _logger.LogInformation("Seed data already present, skipping.");
            return;
        }

        _logger.LogInformation("Seeding demo data (login: alice@example.com / {Password})", DefaultPassword);

        var hash = _passwordHasher.Hash(DefaultPassword);
        var alice = new User("Alice", "Kowalska", "alice@example.com", hash);
        var bob = new User("Bob", "Nowak", "bob@example.com", hash);
        var carol = new User("Carol", "Wiśniewska", "carol@example.com", hash);

        await _unitOfWork.Users.AddAsync(alice, ct);
        await _unitOfWork.Users.AddAsync(bob, ct);
        await _unitOfWork.Users.AddAsync(carol, ct);

        // --- Wycieczka 1: Alice jako organizatorka, Bob + Carol jako uczestnicy ---
        var krakow = new Trip(
            "Weekend w Krakowie",
            "Zwiedzanie Starego Miasta, Wawelu i Kazimierza.",
            new DateRange(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                          new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc)),
            alice.Id);

        krakow.AddParticipant(new Participant(krakow.Id, alice.Id, ParticipantRole.Organizer));
        await _unitOfWork.Trips.AddAsync(krakow, ct);
        await _unitOfWork.AppendAsync(krakow.Id, "TripCreated", alice.Id,
            new { name = krakow.Name }, ct);

        await _unitOfWork.Participants.AddAsync(new Participant(krakow.Id, bob.Id, ParticipantRole.Editor), ct);
        await _unitOfWork.AppendAsync(krakow.Id, "ParticipantInvited", alice.Id,
            new { invitedEmail = bob.Email, role = nameof(ParticipantRole.Editor) }, ct);

        await _unitOfWork.Participants.AddAsync(new Participant(krakow.Id, carol.Id, ParticipantRole.Member), ct);
        await _unitOfWork.AppendAsync(krakow.Id, "ParticipantInvited", alice.Id,
            new { invitedEmail = carol.Email, role = nameof(ParticipantRole.Member) }, ct);

        var wawel = new Attraction(krakow.Id, "Wawel",
            "Zamek Królewski – bilety z przewodnikiem.",
            new Address("Wawel 5", "Kraków", "Polska", "31-001"),
            new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc));
        await _unitOfWork.Attractions.AddAsync(wawel, ct);
        await _unitOfWork.AppendAsync(krakow.Id, "AttractionAdded", bob.Id,
            new { name = wawel.Name, attractionId = wawel.Id }, ct);

        var wieliczka = new Attraction(krakow.Id, "Kopalnia Soli w Wieliczce",
            "Trasa turystyczna (3h).",
            new Address("Daniłowicza 10", "Wieliczka", "Polska", "32-020"),
            new DateTime(2026, 6, 3, 11, 30, 0, DateTimeKind.Utc));
        await _unitOfWork.Attractions.AddAsync(wieliczka, ct);
        await _unitOfWork.AppendAsync(krakow.Id, "AttractionAdded", bob.Id,
            new { name = wieliczka.Name, attractionId = wieliczka.Id }, ct);

        var dinnerPlace = new Attraction(krakow.Id, "Kolacja w Kazimierzu",
            "Rezerwacja na 4 osoby.",
            new Address("Szeroka 12", "Kraków", "Polska", "31-053"),
            new DateTime(2026, 6, 2, 19, 0, 0, DateTimeKind.Utc));
        await _unitOfWork.Attractions.AddAsync(dinnerPlace, ct);
        await _unitOfWork.AppendAsync(krakow.Id, "AttractionAdded", alice.Id,
            new { name = dinnerPlace.Name, attractionId = dinnerPlace.Id }, ct);

        // Nocleg 600 PLN, płaci Alice, dzielimy równo między całą trójkę (200/200/200)
        var accommodation = new Expense(
            krakow.Id, alice.Id,
            "Nocleg – 3 noce",
            "Apartament na Starym Mieście",
            new Money(600m, "PLN"),
            ExpenseCategory.Accommodation,
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        accommodation.AddSplit(new ExpenseSplit(accommodation.Id, alice.Id, new Money(200m, "PLN")));
        accommodation.AddSplit(new ExpenseSplit(accommodation.Id, bob.Id, new Money(200m, "PLN")));
        accommodation.AddSplit(new ExpenseSplit(accommodation.Id, carol.Id, new Money(200m, "PLN")));
        await _unitOfWork.Expenses.AddAsync(accommodation, ct);
        await _unitOfWork.AppendAsync(krakow.Id, "ExpenseAdded", alice.Id,
            new { title = accommodation.Title, amount = 600m, currency = "PLN", expenseId = accommodation.Id }, ct);

        // Obiad 240 PLN, płaci Bob, dzielimy po 80
        var dinner = new Expense(
            krakow.Id, bob.Id,
            "Obiad w Kazimierzu",
            null,
            new Money(240m, "PLN"),
            ExpenseCategory.Food,
            new DateTime(2026, 6, 2, 20, 0, 0, DateTimeKind.Utc));
        dinner.AddSplit(new ExpenseSplit(dinner.Id, alice.Id, new Money(80m, "PLN")));
        dinner.AddSplit(new ExpenseSplit(dinner.Id, bob.Id, new Money(80m, "PLN")));
        dinner.AddSplit(new ExpenseSplit(dinner.Id, carol.Id, new Money(80m, "PLN")));
        await _unitOfWork.Expenses.AddAsync(dinner, ct);
        await _unitOfWork.AppendAsync(krakow.Id, "ExpenseAdded", bob.Id,
            new { title = dinner.Title, amount = 240m, currency = "PLN", expenseId = dinner.Id }, ct);

        // Bilety na Wawel – 150 PLN, bez podziału (po prostu płatnik)
        var tickets = new Expense(
            krakow.Id, carol.Id,
            "Bilety Wawel",
            "3 bilety normalne",
            new Money(150m, "PLN"),
            ExpenseCategory.Activities,
            new DateTime(2026, 6, 2, 9, 30, 0, DateTimeKind.Utc));
        await _unitOfWork.Expenses.AddAsync(tickets, ct);
        await _unitOfWork.AppendAsync(krakow.Id, "ExpenseAdded", carol.Id,
            new { title = tickets.Title, amount = 150m, currency = "PLN", expenseId = tickets.Id }, ct);

        var c1 = new Comment(krakow.Id, alice.Id, "Pamiętajcie o dowodach do rezerwacji!");
        await _unitOfWork.Comments.AddAsync(c1, ct);
        await _unitOfWork.AppendAsync(krakow.Id, "CommentAdded", alice.Id,
            new { commentId = c1.Id, preview = c1.Content }, ct);

        var c2 = new Comment(krakow.Id, bob.Id, "Zarezerwowałem stolik na sobotę wieczór.");
        await _unitOfWork.Comments.AddAsync(c2, ct);
        await _unitOfWork.AppendAsync(krakow.Id, "CommentAdded", bob.Id,
            new { commentId = c2.Id, preview = c2.Content }, ct);

        var c3 = new Comment(krakow.Id, carol.Id, "Kupiłam bilety na Wawel.");
        await _unitOfWork.Comments.AddAsync(c3, ct);
        await _unitOfWork.AppendAsync(krakow.Id, "CommentAdded", carol.Id,
            new { commentId = c3.Id, preview = c3.Content }, ct);

        // --- Wycieczka 2: Bob jako organizator, Alice jako member ---
        var italy = new Trip(
            "Wakacje we Włoszech",
            "Rzym → Florencja → Wenecja.",
            new DateRange(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                          new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Utc)),
            bob.Id);

        italy.AddParticipant(new Participant(italy.Id, bob.Id, ParticipantRole.Organizer));
        await _unitOfWork.Trips.AddAsync(italy, ct);
        await _unitOfWork.AppendAsync(italy.Id, "TripCreated", bob.Id,
            new { name = italy.Name }, ct);

        await _unitOfWork.Participants.AddAsync(new Participant(italy.Id, alice.Id, ParticipantRole.Member), ct);
        await _unitOfWork.AppendAsync(italy.Id, "ParticipantInvited", bob.Id,
            new { invitedEmail = alice.Email, role = nameof(ParticipantRole.Member) }, ct);

        var colosseum = new Attraction(italy.Id, "Koloseum",
            "Bilet z rezerwacją online.",
            new Address("Piazza del Colosseo 1", "Rzym", "Włochy"),
            new DateTime(2026, 7, 16, 9, 0, 0, DateTimeKind.Utc));
        await _unitOfWork.Attractions.AddAsync(colosseum, ct);
        await _unitOfWork.AppendAsync(italy.Id, "AttractionAdded", bob.Id,
            new { name = colosseum.Name, attractionId = colosseum.Id }, ct);

        var uffizi = new Attraction(italy.Id, "Galeria Uffizi",
            null,
            new Address("Piazzale degli Uffizi 6", "Florencja", "Włochy"),
            new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc));
        await _unitOfWork.Attractions.AddAsync(uffizi, ct);
        await _unitOfWork.AppendAsync(italy.Id, "AttractionAdded", bob.Id,
            new { name = uffizi.Name, attractionId = uffizi.Id }, ct);

        var flights = new Expense(
            italy.Id, bob.Id,
            "Loty WAW → FCO",
            "Bilety w obie strony",
            new Money(1800m, "EUR"),
            ExpenseCategory.Transport,
            new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc));
        flights.AddSplit(new ExpenseSplit(flights.Id, alice.Id, new Money(900m, "EUR")));
        flights.AddSplit(new ExpenseSplit(flights.Id, bob.Id, new Money(900m, "EUR")));
        await _unitOfWork.Expenses.AddAsync(flights, ct);
        await _unitOfWork.AppendAsync(italy.Id, "ExpenseAdded", bob.Id,
            new { title = flights.Title, amount = 1800m, currency = "EUR", expenseId = flights.Id }, ct);

        var italyComment = new Comment(italy.Id, bob.Id, "Rezerwacje hoteli wyślę mailem w przyszłym tygodniu.");
        await _unitOfWork.Comments.AddAsync(italyComment, ct);
        await _unitOfWork.AppendAsync(italy.Id, "CommentAdded", bob.Id,
            new { commentId = italyComment.Id, preview = italyComment.Content }, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Seed complete: 3 users, 2 trips, 5 attractions, 4 expenses, 4 comments + matching change log entries in Mongo.");
    }
}
