internal class SchemaValidationResult
{
    public bool Ok { get; internal set; }

    public void Add(SchemaValidationResult newData)
    {
        Ok = Ok && newData.Ok;
    }

    internal Exception ToException()
    {
        throw new Exception();
    }
}
