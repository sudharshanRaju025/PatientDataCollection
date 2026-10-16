using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientDataCollection.Api.Dtos;
using PatientDataCollection.Api.Services;

namespace PatientDataCollection.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/medicines")]
public class MedicinesController(PharmacyService pharmacy) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MedicineListDto>> List(
        [FromQuery] string? search,
        [FromQuery] bool lowStock = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await pharmacy.SearchMedicinesAsync(search, lowStock, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MedicineDto>> Get(int id, CancellationToken cancellationToken)
    {
        var medicine = await pharmacy.GetMedicineAsync(id, cancellationToken);
        return medicine is null ? NotFound(new { message = "That medicine was not found." }) : Ok(medicine);
    }

    [HttpPost]
    public async Task<ActionResult<MedicineDto>> Create(MedicineWriteDto input, CancellationToken cancellationToken)
    {
        var created = await pharmacy.CreateMedicineAsync(input, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MedicineDto>> Update(int id, MedicineWriteDto input, CancellationToken cancellationToken)
    {
        var updated = await pharmacy.UpdateMedicineAsync(id, input, cancellationToken);
        return updated is null ? NotFound(new { message = "That medicine was not found." }) : Ok(updated);
    }
}
