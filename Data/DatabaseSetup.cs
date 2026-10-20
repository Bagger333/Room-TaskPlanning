using Microsoft.EntityFrameworkCore;

namespace Juleplanlaegning.Data;

public static class DatabaseSetup
{
    private const string InitialMigrationId = "20261004105907_InitialCreate";

    // Databases created before migrations were introduced (via EnsureCreated) already have the
    // InitialCreate schema but no migration history. Record that, so Migrate() only applies later migrations.
    public static void BaselineLegacyDatabase(AppDbContext db)
    {
        if (!TableExists(db, "People") || TableExists(db, "__EFMigrationsHistory")) return;

        db.Database.ExecuteSql($"""
            CREATE TABLE "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL)
            """);
        db.Database.ExecuteSql($"""
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ({InitialMigrationId}, '8.0.11')
            """);
    }

    private static bool TableExists(AppDbContext db, string name) =>
        db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = {name}")
            .Single() > 0;
}
