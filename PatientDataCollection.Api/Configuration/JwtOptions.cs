namespace PatientDataCollection.Api.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "MeridianClinical";
    public string Audience { get; set; } = "MeridianClinical";
    public string Key { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 480;
}
