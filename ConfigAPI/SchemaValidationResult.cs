
using System.Text.Json;

public class SchemaValidationResult
{
    public bool Ok { get; internal set; }

    public required  List<ValidationError> Errors { get; init; }

    public void Add(SchemaValidationResult newData)
    {
        Ok = Ok && newData.Ok;
        Errors.AddRange(newData.Errors);
    }

    internal Exception ToException()
    {
        throw new SchemaValidationException(this);
    }

    public class ValidationError
    {
        public string? SchemaName { get; internal set; }
        public IReadOnlyDictionary<string, string>? Errors { get; internal set; }
    }
}

public class SchemaValidationException(SchemaValidationResult result) : Exception()
{
    public SchemaValidationResult Result { get; } = result;
}

