using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.API.DTOs.FormMetadata;
using RPayroll.API.Metadata;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/form-metadata")]
[Authorize]
public class FormMetadataController : ControllerBase
{
    [HttpGet("models")]
    public ActionResult<IEnumerable<FormModelSummaryDto>> GetModels()
    {
        var items = FormModelRegistry.All
            .Select(registration => new FormModelSummaryDto
            {
                Key = registration.Key,
                Label = registration.Label,
                DtoName = registration.DtoType.Name,
                SubmitUrl = registration.SubmitUrl,
                SubmitMethod = registration.SubmitMethod
            })
            .ToList();

        return Ok(items);
    }

    [HttpGet("models/{key}")]
    public ActionResult<FormModelMetadataDto> GetModel(string key)
    {
        var registration = FormModelRegistry.All
            .FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));

        if (registration == null)
        {
            return NotFound();
        }

        return Ok(FormMetadataReader.Build(registration));
    }
}
