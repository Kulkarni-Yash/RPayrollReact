namespace RPayroll.API.DTOs.FormMetadata;

public class FormModelMetadataDto : FormModelSummaryDto
{
    public List<FormFieldMetadataDto> Fields { get; set; } = new();
}
