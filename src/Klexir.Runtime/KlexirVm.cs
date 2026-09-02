using Klexir.Runtime.Abstractions;
using MonadicSharp;

namespace Klexir.Runtime;

/// <summary>
/// Stack-based interpreter for <see cref="OpCode"/> bytecode. Deliberately low-level (raw byte decoding, a plain
/// <see cref="long"/> stack) — this exists to show what a VM does under .NET, not to be a polished public API.
/// </summary>
public sealed class KlexirVm(byte[] code)
{
    public Result<long> Run()
    {
        var stack = new Stack<long>();
        var ip = 0;

        while (true)
        {
            if (ip >= code.Length)
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
