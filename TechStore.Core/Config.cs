namespace TechStore.Core;

public static class Config
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("TECHSTORE_CONN")
        ?? "Server=localhost;Database=TechStoreDB;Trusted_Connection=True;TrustServerCertificate=True;";
}
