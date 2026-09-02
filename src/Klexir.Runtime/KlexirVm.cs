using Klexir.Runtime.Abstractions;
using MonadicSharp;

namespace Klexir.Runtime;

/// <summary>
/// Stack-based interpreter for <see cref="OpCode"/> bytecode. Deliberately low-level (raw byte decoding, a plain
/// <see cref="long"/> value stack and a separate call stack of return addresses) — this exists to show what a VM
/// does under .NET, not to be a polished public API.
/// </summary>
public sealed class KlexirVm(byte[] code, int entryPoint = 0)
{
    /// <summary>The VM's object heap. Exposed so a host can manage GC roots and trigger <see cref="ManagedHeap.Collect"/> around VM execution — <c>Run()</c> never collects on its own.</summary>
    public ManagedHeap Heap { get; } = new();

    public Result<long> Run()
    {
        var stack = new Stack<long>();
        var callStack = new Stack<int>();
        var ip = entryPoint;

        while (true)
        {
            if (ip < 0 || ip >= code.Length)
            {
                return Result<long>.Failure(Error.Create("Bytecode ended without a Halt instruction."));
            }

            var op = (OpCode)code[ip++];

            switch (op)
            {
                case OpCode.Push:
                    if (ip + sizeof(long) > code.Length)
                    {
                        return Result<long>.Failure(Error.Create("Truncated operand for Push."));
                    }

                    stack.Push(BitConverter.ToInt64(code, ip));
                    ip += sizeof(long);
                    break;

                case OpCode.Add:
                case OpCode.Sub:
                case OpCode.Mul:
                case OpCode.Div:
                    var binaryResult = ApplyBinaryOp(op, stack);
                    if (binaryResult.IsFailure)
                    {
                        return Result<long>.Failure(binaryResult.Error);
                    }

                    break;

                case OpCode.Call:
                    if (ip + sizeof(int) > code.Length)
                    {
                        return Result<long>.Failure(Error.Create("Truncated operand for Call."));
                    }

                    var target = BitConverter.ToInt32(code, ip);
                    callStack.Push(ip + sizeof(int));
                    ip = target;
                    break;

                case OpCode.Ret:
                    if (callStack.Count == 0)
                    {
                        return Result<long>.Failure(Error.Create("Ret with no active call."));
                    }

                    ip = callStack.Pop();
                    break;

                case OpCode.NewObj:
                    if (ip + sizeof(int) > code.Length)
                    {
                        return Result<long>.Failure(Error.Create("Truncated operand for NewObj."));
                    }

                    var fieldCount = BitConverter.ToInt32(code, ip);
                    ip += sizeof(int);

                    if (fieldCount < 0)
                    {
                        return Result<long>.Failure(Error.Create("NewObj field count must not be negative."));
                    }

                    stack.Push(Heap.Allocate(fieldCount).Id);
                    break;

                case OpCode.LoadField:
                    if (ip + sizeof(int) > code.Length)
                    {
                        return Result<long>.Failure(Error.Create("Truncated operand for LoadField."));
                    }

                    var loadIndex = BitConverter.ToInt32(code, ip);
                    ip += sizeof(int);

                    if (stack.Count < 1)
                    {
                        return Result<long>.Failure(Error.Create("Stack underflow executing LoadField."));
                    }

                    var loadTarget = new HeapHandle((int)stack.Pop());
                    var loaded = Heap.GetField(loadTarget, loadIndex);
                    if (loaded.IsFailure)
                    {
                        return Result<long>.Failure(loaded.Error);
                    }

                    stack.Push(loaded.Value.Id);
                    break;

                case OpCode.StoreField:
                    if (ip + sizeof(int) > code.Length)
                    {
                        return Result<long>.Failure(Error.Create("Truncated operand for StoreField."));
                    }

                    var storeIndex = BitConverter.ToInt32(code, ip);
                    ip += sizeof(int);

                    if (stack.Count < 2)
                    {
                        return Result<long>.Failure(Error.Create("Stack underflow executing StoreField."));
                    }

                    var fieldValue = new HeapHandle((int)stack.Pop());
                    var storeTarget = new HeapHandle((int)stack.Pop());
                    var stored = Heap.SetField(storeTarget, storeIndex, fieldValue);
                    if (stored.IsFailure)
                    {
                        return Result<long>.Failure(stored.Error);
                    }

                    break;

                case OpCode.Dup:
                    if (stack.Count < 1)
                    {
                        return Result<long>.Failure(Error.Create("Stack underflow executing Dup."));
                    }

                    stack.Push(stack.Peek());
                    break;

                case OpCode.Jump:
                    if (ip + sizeof(int) > code.Length)
                    {
                        return Result<long>.Failure(Error.Create("Truncated operand for Jump."));
                    }

                    ip = BitConverter.ToInt32(code, ip);
                    break;

                case OpCode.JumpIfZero:
                    if (ip + sizeof(int) > code.Length)
                    {
                        return Result<long>.Failure(Error.Create("Truncated operand for JumpIfZero."));
                    }

                    var jumpTarget = BitConverter.ToInt32(code, ip);
                    ip += sizeof(int);

                    if (stack.Count < 1)
                    {
                        return Result<long>.Failure(Error.Create("Stack underflow executing JumpIfZero."));
                    }

                    if (stack.Pop() == 0)
                    {
                        ip = jumpTarget;
                    }

                    break;

                case OpCode.Halt:
                    return stack.Count > 0
                        ? Result<long>.Success(stack.Pop())
                        : Result<long>.Failure(Error.Create("Halt with an empty stack."));

                default:
                    return Result<long>.Failure(Error.Create($"Unknown opcode {(byte)op}."));
            }
        }
    }

    private static Result<Unit> ApplyBinaryOp(OpCode op, Stack<long> stack)
    {
        if (stack.Count < 2)
        {
            return Result<Unit>.Failure(Error.Create($"Stack underflow executing {op}."));
        }

        var right = stack.Pop();
        var left = stack.Pop();

        if (op == OpCode.Div && right == 0)
        {
            return Result<Unit>.Failure(Error.Create("Division by zero."));
        }

        stack.Push(op switch
        {
            OpCode.Add => left + right,
            OpCode.Sub => left - right,
            OpCode.Mul => left * right,
            OpCode.Div => left / right,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Not a binary opcode."),
        });

        return Result<Unit>.Success(Unit.Value);
    }
}
