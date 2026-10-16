namespace PatientDataCollection.Api.Configuration;

public class DatabaseOptions
{
    public const string ConnectionName = "DefaultConnection";

    public string ConnectionString { get; set; } = "";
    public string ServerVersion { get; set; } = "8.0.36-mysql";
    public string Host { get; set; } = "";
    public uint Port { get; set; }
    public string DatabaseName { get; set; } = "";
    public string Schema { get; set; } = "PatientData";
    public string Username { get; set; } = "";
}
