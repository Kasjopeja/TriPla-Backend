namespace TriPla.Backend.Infrastructure.Persistence;

public static class DatabaseSchema
{
    public const string CreateSchemaSql = """
        CREATE TABLE IF NOT EXISTS users (
            id UUID PRIMARY KEY,
            first_name TEXT NOT NULL,
            last_name TEXT NOT NULL,
            email TEXT NOT NULL UNIQUE,
            password_hash TEXT NOT NULL,
            created_at TIMESTAMPTZ NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);

        CREATE TABLE IF NOT EXISTS trips (
            id UUID PRIMARY KEY,
            name TEXT NOT NULL,
            description TEXT NULL,
            start_date TIMESTAMPTZ NOT NULL,
            end_date TIMESTAMPTZ NOT NULL,
            owner_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
            created_at TIMESTAMPTZ NOT NULL,
            updated_at TIMESTAMPTZ NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_trips_owner ON trips(owner_id);

        CREATE TABLE IF NOT EXISTS participants (
            id UUID PRIMARY KEY,
            trip_id UUID NOT NULL REFERENCES trips(id) ON DELETE CASCADE,
            user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
            role INT NOT NULL,
            joined_at TIMESTAMPTZ NOT NULL,
            UNIQUE(trip_id, user_id)
        );

        CREATE INDEX IF NOT EXISTS idx_participants_trip ON participants(trip_id);
        CREATE INDEX IF NOT EXISTS idx_participants_user ON participants(user_id);

        CREATE TABLE IF NOT EXISTS attractions (
            id UUID PRIMARY KEY,
            trip_id UUID NOT NULL REFERENCES trips(id) ON DELETE CASCADE,
            name TEXT NOT NULL,
            description TEXT NULL,
            street TEXT NULL,
            city TEXT NULL,
            country TEXT NULL,
            postal_code TEXT NULL,
            planned_at TIMESTAMPTZ NULL,
            created_at TIMESTAMPTZ NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_attractions_trip ON attractions(trip_id);

        CREATE TABLE IF NOT EXISTS expenses (
            id UUID PRIMARY KEY,
            trip_id UUID NOT NULL REFERENCES trips(id) ON DELETE CASCADE,
            paid_by_user_id UUID NOT NULL REFERENCES users(id),
            title TEXT NOT NULL,
            description TEXT NULL,
            amount NUMERIC(18, 2) NOT NULL,
            currency CHAR(3) NOT NULL,
            category INT NOT NULL,
            date TIMESTAMPTZ NOT NULL,
            created_at TIMESTAMPTZ NOT NULL,
            is_settled BOOLEAN NOT NULL DEFAULT FALSE
        );

        ALTER TABLE expenses ADD COLUMN IF NOT EXISTS is_settled BOOLEAN NOT NULL DEFAULT FALSE;

        CREATE INDEX IF NOT EXISTS idx_expenses_trip ON expenses(trip_id);

        CREATE TABLE IF NOT EXISTS expense_splits (
            id UUID PRIMARY KEY,
            expense_id UUID NOT NULL REFERENCES expenses(id) ON DELETE CASCADE,
            user_id UUID NOT NULL REFERENCES users(id),
            amount NUMERIC(18, 2) NOT NULL,
            currency CHAR(3) NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_expense_splits_expense ON expense_splits(expense_id);

        CREATE TABLE IF NOT EXISTS comments (
            id UUID PRIMARY KEY,
            trip_id UUID NOT NULL REFERENCES trips(id) ON DELETE CASCADE,
            author_id UUID NOT NULL REFERENCES users(id),
            parent_id UUID NULL REFERENCES comments(id) ON DELETE CASCADE,
            content TEXT NOT NULL,
            created_at TIMESTAMPTZ NOT NULL,
            edited_at TIMESTAMPTZ NULL
        );

        -- additive migration for older databases
        ALTER TABLE comments ADD COLUMN IF NOT EXISTS parent_id UUID NULL REFERENCES comments(id) ON DELETE CASCADE;

        CREATE INDEX IF NOT EXISTS idx_comments_trip ON comments(trip_id);
        CREATE INDEX IF NOT EXISTS idx_comments_parent ON comments(parent_id);
    """;
}
