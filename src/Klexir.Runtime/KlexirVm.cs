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
        // A List (not Stack<T>) so LoadLocal can index an arbitrary, already-pushed slot in O(1) — a compiler
        // targeting this VM can bind a `let`'s value to "whatever index it ends up at" and read it back later
        // without popping it, since nothing here ever removes an earlier slot out from under a later one.
        var stack = new List<long>();
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

                    stack.Add(BitConverter.ToInt64(code, ip));
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

                case OpCode.Lt:
                case OpCode.Gt:
                case OpCode.Eq:
                case OpCode.Le:
                case OpCode.Ge:
                    var comparisonResult = ApplyComparisonOp(op, stack);
                    if (comparisonResult.IsFailure)
                    {
                        return Result<long>.Failure(comparisonResult.Error);
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

                    stack.Add(Heap.Allocate(fieldCount).Id);
                    break;

                case OpCode.LoadField:
                    if (ip + sizeof(int) > code.Length)
                    {
                        return Result<long>.Failure(Error.Create("Truncated operand for LoadField."));
                    }

                    var loadIndex = BitConverter.ToInt32(code, ip);
                    ip += sizeof(int);

                    if (!TryPop(stack, out var loadHandleId))
                    {
                        return Result<long>.Failure(Error.Create("Stack underflow executing LoadField."));
                    }

                    var loaded = Heap.GetField(new HeapHandle((int)loadHandleId), loadIndex);
                    if (loaded.IsFailure)
                    {
                        return Result<long>.Failure(loaded.Error);
                    }

                    stack.Add(loaded.Value.Id);
                    break;

                case OpCode.StoreField:
                    if (ip + sizeof(int) > code.Length)
                    {
                        return Result<long>.Failure(Error.Create("Truncated operand for StoreField."));
                    }

                    var storeIndex = BitConverter.ToInt32(code, ip);
                    ip += sizeof(int);

                    if (!TryPop(stack, out var fieldValueId) || !TryPop(stack, out var storeTargetId))
                    {
                        return Result<long>.Failure(Error.Create("Stack underflow executing StoreField."));
                    }

                    var stored = Heap.SetField(new HeapHandle((int)storeTargetId), storeIndex, new HeapHandle((int)fieldValueId));
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

                    stack.Add(stack[^1]);
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

                    if (!TryPop(stack, out var zeroTest))
                    {
                        return Result<long>.Failure(Error.Create("Stack underflow executing JumpIfZero."));
                    }

                    if (zeroTest == 0)
                    {
                        ip = jumpTarget;
                    }

                    break;

                case OpCode.LoadLocal:
                    if (ip + sizeof(int) > code.Length)
                    {
                        return Result<long>.Failure(Error.Create("Truncated operand for LoadLocal."));
                    }

                    var localIndex = BitConverter.ToInt32(code, ip);
                    ip += sizeof(int);

                    if (localIndex < 0 || localIndex >= stack.Count)
                    {
                        return Result<long>.Failure(Error.Create($"LoadLocal index {localIndex} is out of range (stack has {stack.Count} slots)."));
                    }

                    stack.Add(stack[localIndex]);
                    break;

                case OpCode.Halt:
                    return TryPop(stack, out var haltValue)
                        ? Result<long>.Success(haltValue)
                        : Result<long>.Failure(Error.Create("Halt with an empty stack."));

                default:
                    return Result<long>.Failure(Error.Create($"Unknown opcode {(byte)op}."));
            }
        }
    }

    private static bool TryPop(List<long> stack, out long value)
    {
        if (stack.Count == 0)
        {
            value = default;
            return false;
        }

        value = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return true;
    }

    private static Result<Unit> ApplyBinaryOp(OpCode op, List<long> stack)
    {
        if (!TryPop(stack, out var right) || !TryPop(stack, out var left))
        {
            return Result<Unit>.Failure(Error.Create($"Stack underflow executing {op}."));
        }

        if (op == OpCode.Div && right == 0)
        {
            return Result<Unit>.Failure(Error.Create("Division by zero."));
        }

        stack.Add(op switch
        {
            OpCode.Add => left + right,
            OpCode.Sub => left - right,
            OpCode.Mul => left * right,
            OpCode.Div => left / right,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Not a binary opcode."),
        });

        return Result<Unit>.Success(Unit.Value);
    }

    private static Result<Unit> ApplyComparisonOp(OpCode op, List<long> stack)
    {
        if (!TryPop(stack, out var right) || !TryPop(stack, out var left))
        {
            return Result<Unit>.Failure(Error.Create($"Stack underflow executing {op}."));
        }

        var isTrue = op switch
        {
            OpCode.Lt => left < right,
            OpCode.Gt => left > right,
            OpCode.Eq => left == right,
            OpCode.Le => left <= right,
            OpCode.Ge => left >= right,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Not a comparison opcode."),
        };

        stack.Add(isTrue ? 1 : 0);
        return Result<Unit>.Success(Unit.Value);
    }
}
