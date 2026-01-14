using Labyrinth.Items;

namespace LabyrinthTest;

[TestFixture(Description = "Inventory async take and concurrency behavior")]
public class InventoryConcurrencyTest
{
    [Test]
    public async Task ListItemTypesReturnsTypes()
    {
        var inv = new MyInventory(new Key());

        var types = await inv.ListItemTypesAsync();

        using var all = Assert.EnterMultipleScope();

        Assert.That(types, Has.Count.EqualTo(1));
        Assert.That(types.Single(), Is.EqualTo(typeof(Key)));
    }

    [Test]
    public async Task ListItemTypesWithVersionReturnsSameVersionIfInventoryDidNotChange()
    {
        var inv = new MyInventory(new Key());

        var (_, v1) = await inv.ListItemTypesWithVersionAsync();
        var (_, v2) = await inv.ListItemTypesWithVersionAsync();

        Assert.That(v2, Is.EqualTo(v1));
    }

    [Test]
    public async Task ListItemTypesWithVersionVersionChangesAfterMove()
    {
        var from = new MyInventory(new Key());
        var to = new MyInventory();

        var (_, v1) = await from.ListItemTypesWithVersionAsync();

        Assert.That(await to.TryMoveItemsFrom(from, [true]), Is.True);

        var (_, v2) = await from.ListItemTypesWithVersionAsync();
        Assert.That(v2, Is.Not.EqualTo(v1));
    }

    [Test]
    public async Task TakeItemFromSourceMovesTheItem()
    {
        var source = new MyInventory(new Key());
        var bag = new MyInventory();

        var (types, version) = await source.ListItemTypesWithVersionAsync();
        Assert.That(types, Has.Count.EqualTo(1));

        var ok = await bag.TryTakeItemFromAsync(source, itemIndex: 0, expectedSourceVersion: version);

        using var all = Assert.EnterMultipleScope();

        Assert.That(ok, Is.True);
        Assert.That(source.HasItems, Is.False);
        Assert.That(bag.HasItems, Is.True);
        Assert.That((await bag.ListItemTypesAsync()).Single(), Is.EqualTo(typeof(Key)));
    }

    [Test]
    public async Task TakeItemFailsIfSourceInventoryChangedSinceConsultation()
    {
        var source = new MyInventory(new Key());
        var bag = new MyInventory();
        var other = new MyInventory();

        var (_, versionAtConsult) = await source.ListItemTypesWithVersionAsync();

        // Change the source inventory after consultation (simulate concurrent modification)
        Assert.That(await other.TryMoveItemsFrom(source, [true]), Is.True);
        Assert.That(source.HasItems, Is.False);

        // Attempt to take using the old version should fail
        var ok = await bag.TryTakeItemFromAsync(source, itemIndex: 0, expectedSourceVersion: versionAtConsult);

        using var all = Assert.EnterMultipleScope();

        Assert.That(ok, Is.False);
        Assert.That(bag.HasItems, Is.False);
        Assert.That(other.HasItems, Is.True);
    }

    [Test]
    public async Task TakeItemFailsOnInvalidIndex()
    {
        var source = new MyInventory(new Key());
        var bag = new MyInventory();
        var (_, version) = await source.ListItemTypesWithVersionAsync();

        var ok = await bag.TryTakeItemFromAsync(source, itemIndex: 1, expectedSourceVersion: version);

        using var all = Assert.EnterMultipleScope();

        Assert.That(ok, Is.False);
        Assert.That(source.HasItems, Is.True);
        Assert.That(bag.HasItems, Is.False);
    }
}
