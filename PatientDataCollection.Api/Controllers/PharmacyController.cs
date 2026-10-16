using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientDataCollection.Api.Dtos;
using PatientDataCollection.Api.Services;

namespace PatientDataCollection.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/pharmacy")]
public class PharmacyController(PharmacyService pharmacy) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<PharmacySummaryDto>> Summary(CancellationToken cancellationToken)
    {
        return Ok(await pharmacy.GetSummaryAsync(cancellationToken));
    }
}
