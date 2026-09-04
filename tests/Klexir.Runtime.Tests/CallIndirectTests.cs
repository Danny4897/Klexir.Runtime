using FluentAssertions;
using Xunit;

namespace Klexir.Runtime.Tests;

/// <summary>
/// <see cref="OpCode.CallIndirect"/> calls a closure heap object rather than a compile-time-fixed address: field 0
/// is the entry point, fields 1..N are captured values. The calling convention pushes the arguments, then the
/// closure handle on top, then <c>CallIndirect(argCount)</c> — inside the callee, <c>LoadLocal(argCount)</c> reads
/// the closure's own handle back (for upvalue access via <c>LoadField</c>, or to recurse by calling itself).
/// </summary>
public sealed class CallIndirectTests
{
    [Fact]
    public void Run_calls_a_non_capturing_closure_with_one_argument()
    {
        // body: increments its single argument by 1
        var body = BytecodeBuilder.New().LoadLocal(0).Push(1).Add().Ret().Build();

        var main = BytecodeBuilder.New()
            .Push(41)              // argument
            .NewObj(1)             // closure object: field 0 = code address
            .Dup()
            .Push(0)                // body starts at address 0
            .StoreField(0)
            .CallIndirect(1)
            .Halt()
            .Build();

        var (code, entryPoint) = Combine(body, main);
        var result = new KlexirVm(code, entryPoint).Run();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Run_calls_a_closure_that_reads_a_captured_upvalue_via_its_own_handle()
    {
        // body: (its own closure handle).field1 [captured] + (its argument)
        var body = BytecodeBuilder.New()
            .LoadLocal(1)   // own closure handle (slot argCount = 1)
            .LoadField(1)   // captured value
            .LoadLocal(0)   // argument
            .Add()
            .Ret()
            .Build();

        var main = BytecodeBuilder.New()
            .Push(10)       // captured value, sitting at absolute slot 0 outside any call
            .Push(7)        // the call's argument
            .NewObj(2)      // field 0 = code address, field 1 = captured value
            .Dup()
            .Push(0)        // body starts at address 0
            .StoreField(0)
            .Dup()
            .LoadLocal(0)   // reload the captured "10" from slot 0
            .StoreField(1)
            .CallIndirect(1)
            .Halt()
            .Build();

        var (code, entryPoint) = Combine(body, main);
        var result = new KlexirVm(code, entryPoint).Run();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(17);
    }

    [Fact]
    public void Run_computes_a_recursive_factorial_through_a_closure_calling_its_own_handle()
    {
        // body(n): if n < 2 then 1 else n * body(n - 1) — recurses via LoadLocal(1), its own closure handle.
        var body = BytecodeBuilder.New();
        body.LoadLocal(0).Push(2).Lt();
        var elseJump = body.JumpIfZeroPlaceholder();
        body.Push(1).Ret();
        body.PatchInt32(elseJump, body.CurrentAddress);
        body.LoadLocal(0)               // n, kept for the final multiply
            .LoadLocal(0).Push(1).Sub() // n - 1
            .LoadLocal(1)               // own closure handle
            .CallIndirect(1)            // body(n - 1)
            .Mul()
            .Ret();
        var bodyCode = body.Build();

        var main = BytecodeBuilder.New()
            .Push(5)         // argument
            .NewObj(1)       // non-capturing closure: field 0 = code address only
            .Dup()
            .Push(0)         // body starts at address 0
            .StoreField(0)
            .CallIndirect(1)
            .Halt()
            .Build();

        var (code, entryPoint) = Combine(bodyCode, main);
        var result = new KlexirVm(code, entryPoint).Run();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(120);
    }

    [Fact]
    public void Run_fails_on_CallIndirect_stack_underflow()
    {
        var code = BytecodeBuilder.New().Push(0).CallIndirect(1).Halt().Build();

        new KlexirVm(code).Run().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Run_fails_calling_a_handle_that_is_not_a_live_heap_object()
    {
        var code = BytecodeBuilder.New().Push(1).Push(999).CallIndirect(1).Halt().Build();

        new KlexirVm(code).Run().IsFailure.Should().BeTrue();
    }

    private static (byte[] Code, int EntryPoint) Combine(params byte[][] segments)
    {
        var code = segments.SelectMany(s => s).ToArray();
        var entryPoint = segments.Take(segments.Length - 1).Sum(s => s.Length);
        return (code, entryPoint);
    }
}
