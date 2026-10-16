using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientDataCollection.Api.Dtos;
using PatientDataCollection.Api.Services;

namespace PatientDataCollection.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new { message = "Those credentials were not recognized." });
        }

        return Ok(result);
    }
}
