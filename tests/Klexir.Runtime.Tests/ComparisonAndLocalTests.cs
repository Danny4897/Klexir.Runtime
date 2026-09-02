using FluentAssertions;
using Xunit;

namespace Klexir.Runtime.Tests;

public sealed class ComparisonAndLocalTests
{
    [Theory]
    [InlineData(1, 2, 1)] // a < b
    [InlineData(2, 1, 0)]
    [InlineData(2, 2, 0)]
    public void Run_Lt(long a, long b, long expected)
    {
        var code = BytecodeBuilder.New().Push(a).Push(b).Lt().Halt().Build();

        new KlexirVm(code).Run().Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(2, 1, 1)]
    [InlineData(1, 2, 0)]
    [InlineData(2, 2, 0)]
    public void Run_Gt(long a, long b, long expected)
    {
        var code = BytecodeBuilder.New().Push(a).Push(b).Gt().Halt().Build();

        new KlexirVm(code).Run().Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(2, 2, 1)]
    [InlineData(2, 3, 0)]
    public void Run_Eq(long a, long b, long expected)
    {
        var code = BytecodeBuilder.New().Push(a).Push(b).Eq().Halt().Build();

        new KlexirVm(code).Run().Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 2, 1)]
    [InlineData(2, 2, 1)]
    [InlineData(3, 2, 0)]
    public void Run_Le(long a, long b, long expected)
    {
        var code = BytecodeBuilder.New().Push(a).Push(b).Le().Halt().Build();

        new KlexirVm(code).Run().Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(2, 1, 1)]
    [InlineData(2, 2, 1)]
    [InlineData(1, 2, 0)]
    public void Run_Ge(long a, long b, long expected)
    {
        var code = BytecodeBuilder.New().Push(a).Push(b).Ge().Halt().Build();

        new KlexirVm(code).Run().Value.Should().Be(expected);
    }

    [Fact]
    public void Run_comparison_fails_on_stack_underflow()
    {
        var code = BytecodeBuilder.New().Push(1).Lt().Halt().Build();

        new KlexirVm(code).Run().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Run_LoadLocal_reads_a_value_without_removing_it_from_the_stack()
    {
        // slot 0 = 10 (the 'let'-bound value); slot 1 = 5 (another value); reload slot 0 and add it to slot 1's value.
        var code = BytecodeBuilder.New()
            .Push(10)         // index 0
            .Push(5)          // index 1
            .LoadLocal(0)     // pushes a copy of index 0's value (10) — index 0 still holds its original value
            .Add()            // 5 + 10 = 15
            .Halt()
            .Build();

        var result = new KlexirVm(code).Run();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(15);
    }

    [Fact]
    public void Run_LoadLocal_can_read_the_same_slot_more_than_once()
    {
        var code = BytecodeBuilder.New()
            .Push(7)          // index 0
            .LoadLocal(0)
            .LoadLocal(0)
            .Add()            // 7 + 7 = 14
            .Halt()
            .Build();

        new KlexirVm(code).Run().Value.Should().Be(14);
    }

    [Fact]
    public void Run_LoadLocal_fails_for_an_out_of_range_index()
    {
        var code = BytecodeBuilder.New().Push(1).LoadLocal(5).Halt().Build();

        new KlexirVm(code).Run().IsFailure.Should().BeTrue();
    }
}
