namespace E2ETests;

internal static class ServiceUrls
{
    private static string Env(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) ?? fallback;

    public static string Ingestion => Env("INGESTION_URL", "http://localhost:5101");
    public static string Rules => Env("RULES_URL", "http://localhost:5102");
    public static string Alerting => Env("ALERTING_URL", "http://localhost:3100");
}
