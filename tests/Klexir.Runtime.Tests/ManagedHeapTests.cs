using FluentAssertions;
using Xunit;

namespace Klexir.Runtime.Tests;

public sealed class ManagedHeapTests
{
    [Fact]
    public void Allocate_returns_distinct_handles_and_increases_the_object_count()
    {
        var heap = new ManagedHeap();

        var a = heap.Allocate(0);
        var b = heap.Allocate(0);

        a.Should().NotBe(b);
        heap.ObjectCount.Should().Be(2);
    }

    [Fact]
    public void SetField_then_GetField_roundtrips_a_reference()
    {
        var heap = new ManagedHeap();
        var parent = heap.Allocate(1);
        var child = heap.Allocate(0);

        heap.SetField(parent, 0, child);
        var read = heap.GetField(parent, 0);

        read.IsSuccess.Should().BeTrue();
        read.Value.Should().Be(child);
    }

    [Fact]
    public void Collect_frees_an_object_that_no_root_reaches()
    {
        var heap = new ManagedHeap();
        var orphan = heap.Allocate(0);

        var collected = heap.Collect();

        collected.Should().Be(1);
        heap.IsAlive(orphan).Should().BeFalse();
    }

    [Fact]
    public void Collect_keeps_an_object_reachable_through_a_chain_of_references()
    {
        var heap = new ManagedHeap();
        var root = heap.Allocate(1);
        var middle = heap.Allocate(1);
        var leaf = heap.Allocate(0);
        heap.SetField(root, 0, middle);
        heap.SetField(middle, 0, leaf);
        heap.AddRoot(root);

        heap.Collect();

        heap.IsAlive(root).Should().BeTrue();
        heap.IsAlive(middle).Should().BeTrue();
        heap.IsAlive(leaf).Should().BeTrue();
    }

    [Fact]
    public void Collect_frees_a_reference_cycle_that_no_root_reaches()
    {
        var heap = new ManagedHeap();
        var a = heap.Allocate(1);
        var b = heap.Allocate(1);
        heap.SetField(a, 0, b);
        heap.SetField(b, 0, a);

        var collected = heap.Collect();

        collected.Should().Be(2);
        heap.IsAlive(a).Should().BeFalse();
        heap.IsAlive(b).Should().BeFalse();
    }

    [Fact]
    public void GetField_fails_once_the_object_has_been_collected()
    {
        var heap = new ManagedHeap();
        var orphan = heap.Allocate(1);
        heap.Collect();

        var result = heap.GetField(orphan, 0);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemoveRoot_then_Collect_frees_the_previously_rooted_object()
    {
        var heap = new ManagedHeap();
        var obj = heap.Allocate(0);
        heap.AddRoot(obj);
        heap.Collect();
        heap.IsAlive(obj).Should().BeTrue();

        heap.RemoveRoot(obj);
        heap.Collect();

        heap.IsAlive(obj).Should().BeFalse();
    }
}
