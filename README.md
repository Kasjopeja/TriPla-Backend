# TriPla – Backend

[![Backend CI](https://github.com/Kasjopeja/TriPla-Backend/actions/workflows/ci.yml/badge.svg)](https://github.com/Kasjopeja/TriPla-Backend/actions/workflows/ci.yml)
[![CodeQL](https://github.com/Kasjopeja/TriPla-Backend/actions/workflows/codeql.yml/badge.svg)](https://github.com/Kasjopeja/TriPla-Backend/actions/workflows/codeql.yml)

REST API serwujące funkcjonalność aplikacji **TriPla** – wspólnego planowania
podróży. Pozwala tworzyć wycieczki, zapraszać znajomych po e-mailu, planować
atrakcje, śledzić wydatki z automatycznym rozliczeniem oraz prowadzić
dyskusje w komentarzach. Każda zmiana zostaje zapisana w pełnym audit logu.

---

## Spis treści

1. [Stack technologiczny](#stack-technologiczny)
2. [Architektura](#architektura)
3. [Model danych i bazy](#model-danych-i-bazy)
4. [Role i uprawnienia](#role-i-uprawnienia)
5. [Funkcjonalności](#funkcjonalności)
6. [Endpointy REST](#endpointy-rest)
7. [Uruchomienie lokalne](#uruchomienie-lokalne)
8. [Testy](#testy)
9. [Docker i CI/CD](#docker-i-cicd)
10. [Konta demo](#konta-demo)

---

## Stack technologiczny

| Warstwa | Technologia |
|---|---|
| Runtime | **.NET 10** (ASP.NET Core Web API) |
| Architektura | **Clean Architecture** (Domain → Application → Infrastructure → Api) |
| Baza relacyjna | **PostgreSQL 16** – Npgsql / ADO.NET, ręczne zapytania SQL |
| Baza dokumentowa | **MongoDB 7** – audit log zmian wycieczek |
| Autentykacja | **JWT** (HS256) |
| Hashowanie haseł | **BCrypt** (workFactor 12) |
| Dokumentacja API | **OpenAPI / Swagger** |
| Testy | **NUnit 4** + **FluentAssertions** + **Moq** |
| Konteneryzacja | **Docker** (multi-stage) + **docker compose** |
| CI/CD | **GitHub Actions** + **CodeQL** + **Dependabot** + **Trivy** |

---

## Architektura

Projekt podzielony na **cztery niezależne warstwy** w stylu Clean Architecture.
Zależności płyną do środka – Domain nie wie nic o świecie zewnętrznym.

```
TriPla.Backend.Api  →  TriPla.Backend.Application  →  TriPla.Backend.Domain
                                ↑
                  TriPla.Backend.Infrastructure
```

### `TriPla.Backend.Domain`

Czysta logika biznesowa, **bez żadnych zależności zewnętrznych** (poza BCL).

- **Encje** – `Trip`, `User`, `Attraction`, `Expense`, `ExpenseSplit`, `Comment`,
  `Participant`. Każda ma prywatny konstruktor + statyczną fabrykę
  `Rehydrate(...)` używaną przez repo do odtworzenia stanu z bazy bez
  przechodzenia przez walidacje.
- **Value Objects** – `DateRange`, `Money`, `Address`. Niezmienne, walidowane
  w konstruktorze.
- **Enumy** – `ParticipantRole` (Member/Editor/Organizer),
  `ExpenseCategory` (Accommodation/Transport/Food/Activities/Shopping/Other).
- **Interfejsy repozytoriów** – `ITripRepository`, `IUserRepository`,
  `IExpenseRepository`, `IAttractionRepository`, `ICommentRepository`,
  `IParticipantRepository`, `ITripChangeLogRepository`, `IUnitOfWork`.

### `TriPla.Backend.Application`

Przypadki użycia – orkiestruje encje + repozytoria, zwraca `Result<T>`
zamiast rzucania wyjątków.

- **Serwisy** – `TripService`, `AuthService`, `ExpenseService`,
  `AttractionService`, `CommentService`, `ParticipantService`,
  `TripHistoryService`.
- **DTO-tki** pogrupowane wg agregatu (`Trips/`, `Auth/`, `Expenses/`, …).
- **Result\<T\>** (`Common/Result.cs`) – propagacja sukces/porażka bez exceptions
  („railway-oriented programming" lite).
- **ChangeLogExtensions** – jeden helper `IUnitOfWork.AppendAsync(...)` do
  zapisu zdarzeń biznesowych do Mongo.

### `TriPla.Backend.Infrastructure`

Implementacje techniczne.

- **Persistence** – `NpgsqlConnectionFactory`, `DatabaseInitializer`
  (CREATE TABLE IF NOT EXISTS przy starcie + idempotentne ALTER ADD COLUMN
  IF NOT EXISTS dla późniejszych migracji), `DataSeeder` (inicjalne demo dane).
- **Repositories/** – implementacje oparte o **Npgsql** (surowe SQL-e).
  `MongoTripChangeLogRepository` używa MongoDB.Driver.
- **Identity** – `BCryptPasswordHasher`, `JwtTokenProvider`, `JwtOptions`.
- **DependencyInjection.cs** – `AddInfrastructure(config)` rejestruje wszystko.

### `TriPla.Backend.Api`

REST API – cienka warstwa prezentacji.

- **Controllers** – `AuthController`, `TripsController` (zagnieżdżone trasy
  dla atrakcji/wydatków/komentarzy/uczestników/historii).
- **Middleware** – `ExceptionHandlingMiddleware` mapuje nieprzewidziane
  wyjątki domeny na odpowiednie kody HTTP.
- **Extensions** – `GetUserId()` z JWT, `ToActionResult()` z `Result<T>`.
- **Program.cs** – konfiguruje JWT Bearer, Swagger, CORS, w Development
  uruchamia `DatabaseInitializer` + `DataSeeder`.

### Dlaczego nie EF Core / Dapper?

Projekt jest świadomie napisany na **surowym SQL przez Npgsql / ADO.NET**.
Daje to pełną kontrolę nad zapytaniami (JOIN-y, podzapytania, transakcje,
indeksy) i pokazuje znajomość zagadnień bazodanowych.

---

## Model danych i bazy

Aplikacja korzysta z **dwóch baz** – każda do innej roli (CQRS-light):

### PostgreSQL – źródło prawdy

| Tabela | Opis |
|---|---|
| `users` | Konta (e-mail unikalny, hasło BCrypt). |
| `trips` | Wycieczka: nazwa, opis, `DateRange`, owner, timestampy. |
| `participants` | Powiązanie `(trip_id, user_id, role, joined_at)` z `UNIQUE(trip_id, user_id)`. |
| `attractions` | Atrakcje z opcjonalnym `Address`. |
| `expenses` | Wydatek (kwota + waluta, kategoria, płatnik, data, `is_settled`). |
| `expense_splits` | Podział kwot wydatku per użytkownik (ta sama waluta co rodzic). |
| `comments` | Komentarze z opcjonalnym `parent_id` (jednopoziomowe odpowiedzi). |

Schemat trzymany w dwóch miejscach (zsynchronizowanych):

- `TriPla.Backend.Infrastructure/Persistence/DatabaseSchema.cs` – używany
  przez `DatabaseInitializer` przy starcie API.
- `docker/postgres/init/01-schema.sql` – bootstrap przy `docker compose up`
  z pustym wolumenem.

### MongoDB – audit log

Kolekcja `trip_change_log` przechowuje historię zmian każdej wycieczki:

```json
{
  "tripId": "uuid",
  "type": "TripUpdated",
  "actorId": "uuid",
  "actorEmail": "alice@example.com",
  "payload": { "changes": { "name": { "before": "X", "after": "Y" } } },
  "occurredAt": "2026-04-21T12:00:00Z"
}
```

Indeks `{ tripId: 1, occurredAt: -1 }` zapewnia szybki dostęp do listy
zdarzeń z sortowaniem od najnowszych. Każdy serwis aplikacyjny po
wykonanej mutacji woła `_unitOfWork.AppendAsync(tripId, eventType, ...)`
i wpis trafia do Mongo.

---

## Role i uprawnienia

```
       Trip Participant
              ↓
         Trip Editor   (Manage Atractions, Manage Participants)
              ↓
          Trip Owner   (Manage Permissions, Delete Trip)
```

| Akcja | Wymagana rola |
|---|---|
| Podgląd szczegółów wycieczki, kalendarza, historii | Participant |
| Dodawanie wydatków, komentarzy, oznaczanie jako rozliczone | Participant |
| Edycja / usuwanie własnego wydatku lub komentarza | Tylko autor |
| Zarządzanie atrakcjami (Add / Edit / Delete) | Editor + |
| Zaproszenie / usunięcie uczestnika | Editor + |
| Edycja wycieczki (nazwa, opis, daty) | Editor + |
| Bulk „Rozlicz wszystkie wydatki" | Editor + |
| Zmiana roli uczestnika | Owner |
| Usunięcie wycieczki | Owner |

Egzekwowane na poziomie serwisu – każda chroniona metoda przyjmuje
`requestingUserId` i sprawdza `Participant.Role` z bazy.

---

## Funkcjonalności

- **Rejestracja i logowanie** – JWT (HS256), token zawiera `sub`, `email`,
  imię i nazwisko użytkownika.
- **Wycieczki** – tworzenie (twórca staje się Ownerem), edycja, usuwanie,
  lista swoich wycieczek (jako owner LUB uczestnik) z licznikiem osób.
- **Atrakcje** – z opcjonalnym adresem (`Address` value object) i
  zaplanowaną datą.
- **Wydatki** – kwota (`Money` z walidacją ISO 4217), kategoria, płatnik.
  Możliwość:
  - dodania bez podziału (cały koszt = płatnik),
  - automatycznego podziału po równo na wybranych uczestników (z poprawną
    dystrybucją centów żeby suma się zgadzała),
  - dowolnego ręcznego podziału (`splits` z walidacją `sum == amount`).
  Każdy wydatek można **oznaczyć jako rozliczony**; właściciel/edytor
  może rozliczyć wszystko hurtem.
- **Rozliczenie** – frontend liczy uproszczone transfery „kto komu ile"
  per waluta, ignorując rozliczone wydatki.
- **Komentarze** – top-level + jednopoziomowe odpowiedzi (`parent_id`),
  edycja i usuwanie tylko przez autora.
- **Uczestnicy** – zaproszenie po e-mailu (z wyborem roli), usuwanie,
  zmiana roli, samo-opuszczenie wycieczki (Owner nie może opuścić).
- **Historia zmian** – audit log w Mongo z czytelnymi typami zdarzeń
  (`TripUpdated`, `ExpenseAdded`, `RoleChanged`, …) i diff-em zmienionych
  pól dla update'ów (`{ field: { before, after } }`).

---

## Endpointy REST

Wszystkie endpointy poza `/api/auth/*` wymagają nagłówka
`Authorization: Bearer <JWT>`. Pełne przykłady zapytań w
[`TriPla.Backend.Api/TriPla.Backend.Api.http`](./TriPla.Backend.Api/TriPla.Backend.Api.http).

```
POST   /api/auth/register                          { firstName, lastName, email, password }
POST   /api/auth/login                             { email, password }  →  { userId, email, token }

GET    /api/trips                                  lista wycieczek bieżącego usera
POST   /api/trips                                  utwórz wycieczkę (staje się Organizer)
GET    /api/trips/{id}                             szczegóły (participants + attractions + expenses + comments)
PUT    /api/trips/{id}
DELETE /api/trips/{id}

GET    /api/trips/{id}/attractions
POST   /api/trips/{id}/attractions
PUT    /api/trips/attractions/{attractionId}
DELETE /api/trips/attractions/{attractionId}

GET    /api/trips/{id}/expenses
POST   /api/trips/{id}/expenses                    { title, amount, currency, category, splits? }
PUT    /api/trips/expenses/{expenseId}             tylko płatnik
PUT    /api/trips/expenses/{expenseId}/settled     { isSettled }
PUT    /api/trips/{id}/expenses/settled-all        { isSettled }   (Editor+)
DELETE /api/trips/expenses/{expenseId}             tylko płatnik

GET    /api/trips/{id}/comments
POST   /api/trips/{id}/comments                    { content, parentId? }
PUT    /api/trips/comments/{commentId}             tylko autor
DELETE /api/trips/comments/{commentId}             tylko autor

GET    /api/trips/{id}/participants
POST   /api/trips/{id}/participants                { email, role }   (Editor+)
PUT    /api/trips/{id}/participants/{userId}/role  { role }          (Owner)
DELETE /api/trips/{id}/participants/{userId}       (Editor+)
POST   /api/trips/{id}/leave

GET    /api/trips/{id}/history?limit=100           audit log z Mongo
```

Pełna dokumentacja interaktywna pod **`http://localhost:5186/swagger`**.

---

## Uruchomienie lokalne

### Wymagania

- **.NET 10 SDK**
- **Docker Desktop** (do baz danych)
- (opcjonalnie) **JetBrains Rider** lub **Visual Studio**

### 1. Bazy danych

```bash
docker compose up -d            # start Postgres + Mongo
docker compose logs -f          # podgląd logów
docker compose down             # stop
docker compose down -v          # stop + skasowanie wolumenów (re-seed)
```

| Serwis | Host | Port | DB | User | Hasło |
|---|---|---|---|---|---|
| Postgres | `localhost` | `5432` | `tripla` | `postgres` | `postgres` |
| MongoDB | `localhost` | `27017` | `tripla` | `mongo` | `mongo` |

### 2. API

**Z Ridera:** wybierz profil `http: TriPla.Backend.Api` → ▶ → otworzy się
Swagger pod `http://localhost:5186/swagger`.

**Z CLI:**

```bash
cd TriPla.Backend.Api
dotnet run
```

Przy pierwszym starcie w Development API automatycznie:

1. Tworzy schemat (`DatabaseInitializer.InitializeAsync`).
2. Wstawia dane demo, jeśli `alice@example.com` nie istnieje
   (`DataSeeder.SeedAsync`).
3. Inicjalizuje kolekcję `trip_change_log` w Mongo.

---

## Testy

```bash
dotnet test
```

**95 testów** (NUnit), wszystkie zielone. Pokrywają:

- **Domain** – encje (`User`, `Trip`, `Expense`, `Comment`) i value objects
  (`Money`, `DateRange`, `Address`).
- **Application** – wszystkie serwisy przez `InMemoryUnitOfWork` z
  `Tests/Fakes/` (testy permissions, walidacja diff-u, reguł biznesowych).
- **Infrastructure / Identity** – BCrypt password hasher, JWT token provider.

---

## Docker i CI/CD

### Lokalny build obrazu

```bash
docker build -t tripla-api:local -f TriPla.Backend.Api/Dockerfile .
docker run -p 8080:8080 \
  -e ConnectionStrings__Postgres="Host=host.docker.internal;Port=5432;Database=tripla;Username=postgres;Password=postgres" \
  tripla-api:local
```

`Dockerfile` jest **multi-stage**: SDK 10 do restore+publish, runtime
to `aspnet:10.0` z non-root userem na porcie `8080`.

### Pełny stack lokalnie (artefakty z CI)

```bash
# Pobierz tripla-api-image-<sha>.tar i tripla-frontend-image-<sha>.tar z GitHub Actions
docker load -i tripla-api.tar
docker load -i tripla-frontend.tar
docker compose -f docker-compose.prod.yml up
# → frontend: http://localhost:8080, API: http://localhost:8081
```

### GitHub Actions (`.github/workflows/`)

- **`ci.yml`** – build → testy z `.trx` + coverage → docker build (z cache GHA)
  → **Trivy CVE scan** → `docker save` → upload obrazu jako artefakt
  + osobny job `dotnet list package --vulnerable`.
- **`codeql.yml`** – statyczna analiza C# (`security-extended`),
  schedule co poniedziałek.
- **Concurrency** – nowy push na ten sam branch anuluje poprzedni run.

### Dependabot

`.github/dependabot.yml` – tygodniowe PR-y dla NuGet, GitHub Actions
i bazowych Docker imagów. Aktualizacje pogrupowane (`Microsoft.*`,
`testing` itd.) żeby nie zalewać repo.

---

## Konta demo

Hasło dla wszystkich: **`Password123!`**

| E-mail | Imię | Rola w demo |
|---|---|---|
| `alice@example.com` | Alice Kowalska | Organizer „Weekend w Krakowie" + member „Wakacje we Włoszech" |
| `bob@example.com` | Bob Nowak | Editor Krakowa + Organizer Włoch |
| `carol@example.com` | Carol Wiśniewska | Member Krakowa |

### „Weekend w Krakowie" (01–04.06.2026)

- 3 atrakcje: Wawel, Kopalnia Soli Wieliczka, Kolacja w Kazimierzu
- 3 wydatki: Nocleg 600 PLN (split 200/200/200, Alice), Obiad 240 PLN
  (split 80/80/80, Bob), Bilety Wawel 150 PLN (Carol, bez podziału)
- 3 komentarze
- Pełna historia zmian od momentu utworzenia

### „Wakacje we Włoszech" (15–25.07.2026)

- 2 atrakcje: Koloseum, Galeria Uffizi
- 1 wydatek: Loty 1800 EUR (split 900/900, Bob)
- 1 komentarz

---

## Podgląd danych

**Postgres CLI:**

```bash
docker exec -it tripla-postgres psql -U postgres -d tripla
\dt                              -- lista tabel
SELECT * FROM users;
SELECT name, start_date, end_date FROM trips;
```

**Mongo CLI:**

```bash
docker exec -it tripla-mongo mongosh -u mongo -p mongo --authenticationDatabase admin
use tripla
db.trip_change_log.find().sort({occurredAt: -1}).pretty()
```

**JetBrains Database Tool:** host `localhost`, port `5432`, db `tripla`,
user `postgres`, hasło `postgres`.
