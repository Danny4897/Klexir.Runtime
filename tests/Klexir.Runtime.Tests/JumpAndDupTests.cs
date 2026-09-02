using FluentAssertions;
using Xunit;

namespace Klexir.Runtime.Tests;

public sealed class JumpAndDupTests
{
    [Fact]
    public void Run_Dup_duplicates_the_top_of_stack()
    {
        var code = BytecodeBuilder.New().Push(9).Dup().Add().Halt().Build();

        var result = new KlexirVm(code).Run();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(18);
    }

    [Fact]
    public void Run_Dup_fails_on_an_empty_stack()
    {
        var code = BytecodeBuilder.New().Dup().Halt().Build();

        new KlexirVm(code).Run().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Run_Jump_skips_the_bytecode_between_it_and_its_patched_target()
    {
        var builder = BytecodeBuilder.New().Push(1);
        var jumpOperand = builder.JumpPlaceholder();
        builder.Push(999); // skipped
        var target = builder.CurrentAddress;
        builder.PatchInt32(jumpOperand, target);
        builder.Push(2).Halt();

        var result = new KlexirVm(builder.Build()).Run();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact]
    public void Run_JumpIfZero_jumps_when_the_top_of_stack_is_zero()
    {
        var builder = BytecodeBuilder.New().Push(0);
        var jumpOperand = builder.JumpIfZeroPlaceholder();
        builder.Push(999); // skipped
        var target = builder.CurrentAddress;
        builder.PatchInt32(jumpOperand, target);
        builder.Push(2).Halt();

        new KlexirVm(builder.Build()).Run().Value.Should().Be(2);
    }

    [Fact]
    public void Run_JumpIfZero_falls_through_when_the_top_of_stack_is_nonzero()
    {
        var builder = BytecodeBuilder.New().Push(7);
        var jumpOperand = builder.JumpIfZeroPlaceholder();
        builder.Push(2).Halt();
        builder.PatchInt32(jumpOperand, builder.CurrentAddress); // never taken; target value is irrelevant

        new KlexirVm(builder.Build()).Run().Value.Should().Be(2);
    }

    [Fact]
    public void Run_a_backward_jump_loop_counts_down_to_zero_and_halts()
    {
        // while (counter != 0) { counter -= 1 } — proves Dup + JumpIfZero + a backward Jump compose into a real loop.
        var builder = BytecodeBuilder.New().Push(5);
        var loopStart = builder.CurrentAddress;
        builder.Dup();
        var exitJumpOperand = builder.JumpIfZeroPlaceholder();
        builder.Push(1).Sub().Jump(loopStart);
        builder.PatchInt32(exitJumpOperand, builder.CurrentAddress);
        builder.Halt();

        var result = new KlexirVm(builder.Build()).Run();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }
}
