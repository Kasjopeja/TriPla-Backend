// =============================================================
//  TriPla – inicjalizacja MongoDB (uruchamiane przez
//  docker-entrypoint-initdb.d przy pierwszym starcie kontenera).
//
//  Aplikacja używa PostgreSQL jako głównej bazy (write model),
//  a MongoDB trzyma audit log / historię zmian wycieczek (read
//  model dla "View Change History" z diagramu).
//
//  Ten skrypt tworzy bazę, użytkownika aplikacyjnego oraz
//  kolekcję `trip_change_log` z indeksem. Demo danych NIE
//  seedujemy tutaj – robi to DataSeeder w C#, żeby tripId
//  w Mongo pokrywało się z tym w Postgresie.
// =============================================================

db = db.getSiblingDB('tripla');

// Użytkownik aplikacyjny z uprawnieniami tylko do bazy `tripla`.
db.createUser({
    user: 'tripla_app',
    pwd: 'tripla_app',
    roles: [{ role: 'readWrite', db: 'tripla' }]
});

// Kolekcja: log zmian wycieczek.
db.createCollection('trip_change_log');
db.trip_change_log.createIndex({ tripId: 1, occurredAt: -1 });

print('[tripla-mongo] Inicjalizacja zakończona. Kolekcja trip_change_log gotowa.');
