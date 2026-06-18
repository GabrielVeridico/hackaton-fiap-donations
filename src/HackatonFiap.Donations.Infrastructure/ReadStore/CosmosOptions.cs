namespace HackatonFiap.Donations.Infrastructure.ReadStore;

public sealed class CosmosOptions
{
    public const string SectionName = "Cosmos";

    public string ConnectionString { get; set; } = string.Empty;
    public string Database { get; set; } = "HackatonFiapDonations";
    public string Container { get; set; } = "campaigns";
}
