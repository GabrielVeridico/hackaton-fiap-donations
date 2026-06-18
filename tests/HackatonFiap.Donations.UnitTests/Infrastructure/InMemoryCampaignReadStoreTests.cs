using FluentAssertions;
using HackatonFiap.Donations.Application.ReadModels;
using HackatonFiap.Donations.Infrastructure.ReadStore;
using Xunit;

namespace HackatonFiap.Donations.UnitTests.Infrastructure;

public class InMemoryCampaignReadStoreTests
{
    [Fact]
    public async Task Upsert_replaces_by_id_and_list_active_filters_non_active()
    {
        var store = new InMemoryCampaignReadStore();
        var id = Guid.NewGuid();

        await store.UpsertAsync(new CampaignReadModel(id, "A", 100m, 10m, "Active"));
        await store.UpsertAsync(new CampaignReadModel(id, "A", 100m, 50m, "Active")); // upsert mesmo id
        await store.UpsertAsync(new CampaignReadModel(Guid.NewGuid(), "B", 100m, 0m, "Completed"));

        var active = await store.ListActiveAsync();

        active.Should().HaveCount(1);
        active[0].AmountRaised.Should().Be(50m);
    }
}
