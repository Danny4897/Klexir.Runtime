using FluentAssertions;
using Xunit;

namespace Klexir.Runtime.Tests;

public sealed class HeapOpcodeTests
{
    [Fact]
    public void Run_creates_an_object_and_leaves_its_handle_id_on_the_stack()
    {
        var code = BytecodeBuilder.New().NewObj(2).Halt().Build();

        var result = new KlexirVm(code).Run();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public void Run_stores_and_loads_a_field_roundtripping_a_handle()
    {
        var code = BytecodeBuilder.New()
            .NewObj(1) // handle 0, stack: [0]
            .NewObj(0) // handle 1, stack: [0, 1]
            .StoreField(0) // obj0.field0 = handle1, stack: []
            .Push(0) // re-push the known id of obj0, stack: [0]
            .LoadField(0) // stack: [1]
            .Halt()
            .Build();

        var result = new KlexirVm(code).Run();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
    }

    [Fact]
    public void Run_fails_loading_a_field_from_an_unallocated_handle()
    {
        var code = BytecodeBuilder.New().Push(999).LoadField(0).Halt().Build();

        var result = new KlexirVm(code).Run();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Run_fails_on_a_negative_field_count_for_NewObj()
    {
        var code = BytecodeBuilder.New().NewObj(-1).Halt().Build();

        var result = new KlexirVm(code).Run();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void The_vm_exposes_its_heap_for_external_root_management_and_collection()
    {
        var code = BytecodeBuilder.New().NewObj(0).Halt().Build();
        var vm = new KlexirVm(code);
        vm.Run();

        vm.Heap.ObjectCount.Should().Be(1);

        vm.Heap.Collect();

        vm.Heap.ObjectCount.Should().Be(0);
    }
}
