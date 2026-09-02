using FluentAssertions;
using Xunit;

namespace Klexir.Runtime.Tests;

public sealed class KlexirVmTests
{
    [Fact]
    public void Run_computes_push_push_add_halt()
    {
        var code = BytecodeBuilder.New().Push(5).Push(3).Add().Halt().Build();

        var result = new KlexirVm(code).Run();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(8);
    }

    [Theory]
    [InlineData(10, 4, 6)]
    public void Run_computes_subtraction_in_push_order(long a, long b, long expected)
    {
        var code = BytecodeBuilder.New().Push(a).Push(b).Sub().Halt().Build();

        var result = new KlexirVm(code).Run();

        result.Value.Should().Be(expected);
    }

    [Fact]
    public void Run_computes_multiplication()
    {
        var code = BytecodeBuilder.New().Push(6).Push(7).Mul().Halt().Build();

        new KlexirVm(code).Run().Value.Should().Be(42);
    }

    [Fact]
    public void Run_computes_division_in_push_order()
    {
        var code = BytecodeBuilder.New().Push(20).Push(4).Div().Halt().Build();

        new KlexirVm(code).Run().Value.Should().Be(5);
    }

    [Fact]
    public void Run_fails_on_division_by_zero()
    {
        var code = BytecodeBuilder.New().Push(1).Push(0).Div().Halt().Build();

        var result = new KlexirVm(code).Run();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Run_fails_on_stack_underflow()
    {
        var code = BytecodeBuilder.New().Push(1).Add().Halt().Build();

        var result = new KlexirVm(code).Run();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Run_fails_when_bytecode_has_no_halt()
    {
        var code = BytecodeBuilder.New().Push(1).Build();

        var result = new KlexirVm(code).Run();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Run_fails_on_halt_with_an_empty_stack()
    {
        var code = BytecodeBuilder.New().Halt().Build();

        var result = new KlexirVm(code).Run();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Run_fails_on_an_unknown_opcode()
    {
        var code = new byte[] { 0xFF };

        var result = new KlexirVm(code).Run();

        result.IsFailure.Should().BeTrue();
    }
}
