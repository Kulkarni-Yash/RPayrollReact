namespace RPayroll.API.DTOs.FormMetadata;

public class FormFieldMetadataDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool Required { get; set; }
    public bool Nullable { get; set; }
}
