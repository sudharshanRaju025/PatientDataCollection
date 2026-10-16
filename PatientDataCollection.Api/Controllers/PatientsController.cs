using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientDataCollection.Api.Dtos;
using PatientDataCollection.Api.Services;

namespace PatientDataCollection.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/patients")]
public class PatientsController(PatientService patients) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<PatientStatsDto>> Stats(CancellationToken cancellationToken)
    {
        return Ok(await patients.GetStatsAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<PatientListDto>> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 8,
        CancellationToken cancellationToken = default)
    {
        return Ok(await patients.SearchAsync(search, status, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PatientDetailDto>> Get(int id, CancellationToken cancellationToken)
    {
        var patient = await patients.GetAsync(id, cancellationToken);
        return patient is null ? NotFound(new { message = "That patient record was not found." }) : Ok(patient);
    }

    [HttpPost]
    public async Task<ActionResult<PatientDetailDto>> Create(PatientWriteDto input, CancellationToken cancellationToken)
    {
        var created = await patients.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PatientDetailDto>> Update(int id, PatientWriteDto input, CancellationToken cancellationToken)
    {
        var updated = await patients.UpdateAsync(id, input, cancellationToken);
        return updated is null ? NotFound(new { message = "That patient record was not found." }) : Ok(updated);
    }
}
