using ConfigAPI;
using System.Text.Json.Nodes;

namespace Tests;

public class SchemaServiceTests
{
    private readonly SchemaService sut;

    public SchemaServiceTests()
    {
        sut = new SchemaService();
    }

    [Fact]
    public async Task Test1()
    {
        await sut.Set("*", JsonValue.Create("hello"));
        await sut.Set("test.*", JsonValue.Create("5"));
        await sut.Set("test.hello", JsonValue.Create(false));
        await sut.Set("*.*.*", JsonValue.Create("hello***"));
        await sut.Set("7.3.1", new JsonArray([JsonValue.Create(5)]));
        await sut.Set("fiejfi", new JsonObject(new Dictionary<string, JsonNode?>
        {
            ["okey"] = JsonValue.Create(5)
        }));


        var result = await sut.Get("test.hello");

        Assert.Equal(2, result.Count());
    }
}
