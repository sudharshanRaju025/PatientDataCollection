using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientDataCollection.Api.Dtos;
using PatientDataCollection.Api.Services;

namespace PatientDataCollection.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/prescriptions")]
public class PrescriptionsController(PharmacyService pharmacy) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PrescriptionListDto>> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? patientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 8,
        CancellationToken cancellationToken = default)
    {
        return Ok(await pharmacy.SearchPrescriptionsAsync(search, status, patientId, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PrescriptionDetailDto>> Get(int id, CancellationToken cancellationToken)
    {
        var prescription = await pharmacy.GetPrescriptionAsync(id, cancellationToken);
        return prescription is null ? NotFound(new { message = "That prescription was not found." }) : Ok(prescription);
    }

    [HttpPost]
    public async Task<ActionResult<PrescriptionDetailDto>> Create(PrescriptionWriteDto input, CancellationToken cancellationToken)
    {
        try
        {
            var created = await pharmacy.CreatePrescriptionAsync(input, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/dispense")]
    public async Task<ActionResult<PrescriptionDetailDto>> Dispense(int id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await pharmacy.DispenseAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<PrescriptionDetailDto>> Cancel(int id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await pharmacy.CancelAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
