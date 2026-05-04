namespace TriPla.Backend.Infrastructure.Persistence;

public class MongoOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
}
