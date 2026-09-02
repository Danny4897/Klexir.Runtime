namespace Klexir.Runtime;

/// <summary>Reference to a heap-allocated object. <see cref="None"/> represents an empty (unset) field.</summary>
public readonly record struct HeapHandle(int Id)
{
    public static readonly HeapHandle None = new(-1);

    public override string ToString() => Id.ToString();
}
