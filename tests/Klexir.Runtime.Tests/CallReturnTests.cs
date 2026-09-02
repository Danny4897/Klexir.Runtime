using FluentAssertions;
using Xunit;

namespace Klexir.Runtime.Tests;

public sealed class CallReturnTests
{
    [Fact]
    public void Run_executes_a_call_and_return_leaving_the_subroutines_result_on_the_stack()
    {
        var subroutine = BytecodeBuilder.New().Push(100).Ret().Build();
        var main = BytecodeBuilder.New().Call(0).Halt().Build();
        var (code, entryPoint) = Combine(subroutine, main);

        var result = new KlexirVm(code, entryPoint).Run();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(100);
    }

    [Fact]
    public void Run_supports_a_call_nested_inside_another_call()
    {
        var subB = BytecodeBuilder.New().Push(7).Ret().Build();
        var subA = BytecodeBuilder.New().Call(0).Push(3).Add().Ret().Build();
        var main = BytecodeBuilder.New().Call(subB.Length).Halt().Build();
        var (code, entryPoint) = Combine(subB, subA, main);

        var result = new KlexirVm(code, entryPoint).Run();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10);
    }

    [Fact]
    public void Run_fails_on_a_return_with_no_active_call()
    {
        var code = BytecodeBuilder.New().Ret().Build();

        var result = new KlexirVm(code).Run();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Run_fails_on_a_call_to_an_out_of_range_address()
    {
        var code = BytecodeBuilder.New().Call(9999).Halt().Build();

        var result = new KlexirVm(code).Run();

        result.IsFailure.Should().BeTrue();
    }

    private static (byte[] Code, int EntryPoint) Combine(params byte[][] segments)
    {
        var code = segments.SelectMany(s => s).ToArray();
        var entryPoint = segments.Take(segments.Length - 1).Sum(s => s.Length);
        return (code, entryPoint);
    }
}
