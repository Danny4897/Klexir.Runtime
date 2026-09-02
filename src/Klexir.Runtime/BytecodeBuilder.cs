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
