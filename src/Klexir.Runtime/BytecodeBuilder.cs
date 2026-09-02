using Klexir.Runtime.Abstractions;

namespace Klexir.Runtime;

/// <summary>Fluent assembler that emits bytes matching <see cref="OpCode"/>'s layout.</summary>
public sealed class BytecodeBuilder
{
    private readonly List<byte> _bytes = [];

    public static BytecodeBuilder New() => new();

    public BytecodeBuilder Push(long value)
    {
        _bytes.Add((byte)OpCode.Push);
        _bytes.AddRange(BitConverter.GetBytes(value));
        return this;
    }

    public BytecodeBuilder Add() => Emit(OpCode.Add);

    public BytecodeBuilder Sub() => Emit(OpCode.Sub);

    public BytecodeBuilder Mul() => Emit(OpCode.Mul);

    public BytecodeBuilder Div() => Emit(OpCode.Div);

    public BytecodeBuilder Halt() => Emit(OpCode.Halt);

    public BytecodeBuilder Call(int targetAddress)
    {
        _bytes.Add((byte)OpCode.Call);
        _bytes.AddRange(BitConverter.GetBytes(targetAddress));
        return this;
    }

    public BytecodeBuilder Ret() => Emit(OpCode.Ret);

    public BytecodeBuilder NewObj(int fieldCount) => EmitWithInt32Operand(OpCode.NewObj, fieldCount);

    public BytecodeBuilder LoadField(int fieldIndex) => EmitWithInt32Operand(OpCode.LoadField, fieldIndex);

    public BytecodeBuilder StoreField(int fieldIndex) => EmitWithInt32Operand(OpCode.StoreField, fieldIndex);

    public BytecodeBuilder Dup() => Emit(OpCode.Dup);

    public BytecodeBuilder Lt() => Emit(OpCode.Lt);

    public BytecodeBuilder Gt() => Emit(OpCode.Gt);

    public BytecodeBuilder Eq() => Emit(OpCode.Eq);

    public BytecodeBuilder Le() => Emit(OpCode.Le);

    public BytecodeBuilder Ge() => Emit(OpCode.Ge);

    public BytecodeBuilder LoadLocal(int index) => EmitWithInt32Operand(OpCode.LoadLocal, index);

    public BytecodeBuilder Jump(int targetAddress) => EmitWithInt32Operand(OpCode.Jump, targetAddress);

    /// <summary>The byte offset the next emitted instruction will start at — capture it before emitting a forward jump's target.</summary>
    public int CurrentAddress => _bytes.Count;

    /// <summary>Emits a Jump with a zero placeholder operand; patch it once the target address is known via <see cref="PatchInt32"/>.</summary>
    public int JumpPlaceholder() => EmitPlaceholder(OpCode.Jump);

    /// <summary>Emits a JumpIfZero with a zero placeholder operand; patch it once the target address is known via <see cref="PatchInt32"/>.</summary>
    public int JumpIfZeroPlaceholder() => EmitPlaceholder(OpCode.JumpIfZero);

    /// <summary>Overwrites a 4-byte operand previously emitted at <paramref name="operandPosition"/> (as returned by <see cref="JumpPlaceholder"/>/<see cref="JumpIfZeroPlaceholder"/>).</summary>
    public void PatchInt32(int operandPosition, int value)
    {
        var bytes = BitConverter.GetBytes(value);
        for (var i = 0; i < bytes.Length; i++)
        {
            _bytes[operandPosition + i] = bytes[i];
        }
    }

    private int EmitPlaceholder(OpCode op)
    {
        _bytes.Add((byte)op);
        var operandPosition = _bytes.Count;
        _bytes.AddRange(BitConverter.GetBytes(0));
        return operandPosition;
    }

    private BytecodeBuilder EmitWithInt32Operand(OpCode op, int operand)
    {
        _bytes.Add((byte)op);
        _bytes.AddRange(BitConverter.GetBytes(operand));
        return this;
    }

    private BytecodeBuilder Emit(OpCode op)
    {
        _bytes.Add((byte)op);
        return this;
    }

    public byte[] Build() => [.. _bytes];
}
