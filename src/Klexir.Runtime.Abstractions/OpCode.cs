namespace Klexir.Runtime.Abstractions;

/// <summary>
/// Instruction set for the Klexir stack-based bytecode VM. This byte layout is the compatibility contract between
/// a bytecode emitter (Klexir.Lang) and the interpreter (Klexir.Runtime) — changing a value here is a breaking change.
/// </summary>
public enum OpCode : byte
{
    /// <summary>Pushes the following 8-byte little-endian <see cref="long"/> operand onto the stack.</summary>
    Push = 0,

    /// <summary>Pops two values, pushes their sum.</summary>
    Add = 1,

    /// <summary>Pops two values (b, a in pop order), pushes a - b.</summary>
    Sub = 2,

    /// <summary>Pops two values, pushes their product.</summary>
    Mul = 3,

    /// <summary>Pops two values (b, a in pop order), pushes a / b. Fails if b is zero.</summary>
    Div = 4,

    /// <summary>Stops execution and returns the top of stack.</summary>
    Halt = 5,

    /// <summary>Pushes the instruction pointer following this instruction's 4-byte operand onto the call stack, then jumps to that operand's absolute address.</summary>
    Call = 6,

    /// <summary>Pops the call stack and jumps to that return address. Fails if the call stack is empty.</summary>
    Ret = 7,
}
