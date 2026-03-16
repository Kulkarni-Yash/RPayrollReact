using System.ComponentModel.DataAnnotations;
using System.Reflection;
using RPayroll.API.DTOs.FormMetadata;

namespace RPayroll.API.Metadata;

public static class FormMetadataReader
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    public static FormModelMetadataDto Build(FormModelRegistration registration)
    {
        var fields = registration.DtoType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.CanWrite)
            .Where(IsSupportedProperty)
            .Select(ToFieldMetadata)
            .ToList();

        return new FormModelMetadataDto
        {
            Key = registration.Key,
            Label = registration.Label,
            DtoName = registration.DtoType.Name,
            SubmitUrl = registration.SubmitUrl,
            SubmitMethod = registration.SubmitMethod,
            Fields = fields
        };
    }

    private static bool IsSupportedProperty(PropertyInfo property)
    {
        var propertyType = UnwrapNullable(property.PropertyType);

        if (propertyType.IsEnum)
        {
            return true;
        }

        if (propertyType == typeof(string) ||
            propertyType == typeof(bool) ||
            propertyType == typeof(int) ||
            propertyType == typeof(long) ||
            propertyType == typeof(short) ||
            propertyType == typeof(decimal) ||
            propertyType == typeof(float) ||
            propertyType == typeof(double) ||
            propertyType == typeof(DateTime) ||
            propertyType == typeof(TimeSpan))
        {
            return true;
        }

        return false;
    }

    private static FormFieldMetadataDto ToFieldMetadata(PropertyInfo property)
    {
        var propertyType = UnwrapNullable(property.PropertyType);
        var nullability = NullabilityContext.Create(property);
        var isNullable = property.PropertyType.IsClass
            ? nullability.ReadState != NullabilityState.NotNull
            : Nullable.GetUnderlyingType(property.PropertyType) != null;

        var required = property.GetCustomAttribute<RequiredAttribute>() != null ||
                       (!isNullable && propertyType != typeof(bool));

        return new FormFieldMetadataDto
        {
            Key = property.Name,
            Label = SplitPascalCase(property.Name),
            DataType = MapDataType(propertyType),
            Required = required,
            Nullable = isNullable
        };
    }

    private static Type UnwrapNullable(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    private static string MapDataType(Type type)
    {
        if (type.IsEnum)
        {
            return "lookup";
        }

        if (type == typeof(DateTime))
        {
            return "date";
        }

        if (type == typeof(TimeSpan))
        {
            return "datetime";
        }

        if (type == typeof(bool))
        {
            return "boolean";
        }

        if (type == typeof(int) ||
            type == typeof(long) ||
            type == typeof(short) ||
            type == typeof(decimal) ||
            type == typeof(float) ||
            type == typeof(double))
        {
            return "number";
        }

        return "string";
    }

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character)
                ? $" {character}"
                : character.ToString()));
    }
}
