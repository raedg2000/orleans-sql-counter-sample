namespace OrleansGrains.Model;

public static class WellKnownCounterIds
{
    public static readonly Guid Counter1 = new("9ec79b44-967b-4684-87ab-1b3f919054a7");
    public static readonly Guid Counter2 = new("a0eff5e9-fa0d-471e-9100-c5f9335d6051");
    public static readonly Guid Counter3 = new("83519673-3e02-4aa2-98f6-e51207d242ef");
    public static readonly Guid Counter4 = new("1651f451-4519-4732-8029-3b360c0226b2");
    public static readonly Guid Counter5 = new("0268cc26-4ad4-4cae-8b9f-8379d3db2979");

    public static readonly IReadOnlyList<(Guid Id, string Name)> All =
    [
        (Counter1, "Counter One"),
        (Counter2, "Counter Two"),
        (Counter3, "Counter Three"),
        (Counter4, "Counter Four"),
        (Counter5, "Counter Five"),
    ];
}
