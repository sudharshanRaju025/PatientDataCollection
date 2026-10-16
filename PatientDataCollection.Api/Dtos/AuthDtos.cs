using System.ComponentModel.DataAnnotations;

namespace PatientDataCollection.Api.Dtos;

public class LoginRequest
{
    [Required, MaxLength(80)]
    public string Username { get; set; } = "";

    [Required, MaxLength(200)]
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
}
