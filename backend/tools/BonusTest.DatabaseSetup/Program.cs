using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

var root = Path.GetFullPath(args.FirstOrDefault() ?? Directory.GetCurrentDirectory());
var passwordEntry = File.ReadAllLines(Path.Combine(root, ".env"))
    .FirstOrDefault(x => x.StartsWith("MSSQL_SA_PASSWORD="))
    ?? throw new InvalidOperationException("Falta MSSQL_SA_PASSWORD en .env.");
var builder = new SqlConnectionStringBuilder
{
    DataSource = "tcp:127.0.0.1,1433", InitialCatalog = "master", UserID = "sa",
    Password = passwordEntry["MSSQL_SA_PASSWORD=".Length..], TrustServerCertificate = true,
    ConnectTimeout = 15
};
await using var connection = new SqlConnection(builder.ConnectionString);
await connection.OpenAsync();

await using (var probe = new SqlCommand("SELECT DB_ID(N'BonusTest')", connection))
{
    if (await probe.ExecuteScalarAsync() is DBNull)
        await RunScriptAsync("01_schema_seed.sql");
    else
        Console.WriteLine("BonusTest ya existe; se conserva sin ejecutar de nuevo el seed.");
}
await connection.ChangeDatabaseAsync("BonusTest");
await using (var probe = new SqlCommand("SELECT OBJECT_ID(N'dbo.Employee', N'U')", connection))
{
    if (await probe.ExecuteScalarAsync() is DBNull)
        throw new InvalidOperationException("La base existe pero no contiene Employee. Revisar su estado; no se borró nada.");
}
await using (var probe = new SqlCommand("SELECT OBJECT_ID(N'dbo.Bonus', N'U')", connection))
{
    if (await probe.ExecuteScalarAsync() is DBNull)
        await RunScriptAsync("02_bonus.sql");
    else
        Console.WriteLine("Bonus ya existe; se conserva sin recrearla.");
}
Console.WriteLine("SQL Server preparado: localhost,1433 / BonusTest.");

async Task RunScriptAsync(string file)
{
    var sql = await File.ReadAllTextAsync(Path.Combine(root, "database", file));
    foreach (var batch in Regex.Split(sql, @"^\s*GO\s*\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(batch)) continue;
        await using var command = new SqlCommand(batch, connection) { CommandTimeout = 60 };
        await command.ExecuteNonQueryAsync();
    }
    Console.WriteLine($"Ejecutado {file}.");
}

