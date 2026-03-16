namespace RPayroll.API.DTOs.FormMetadata;

public class FormModelSummaryDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DtoName { get; set; } = string.Empty;
    public string SubmitUrl { get; set; } = string.Empty;
    public string SubmitMethod { get; set; } = string.Empty;
}
