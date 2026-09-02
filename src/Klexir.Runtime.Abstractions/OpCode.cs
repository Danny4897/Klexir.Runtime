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

    /// <summary>Allocates a heap object with the following 4-byte field count; pushes its handle id.</summary>
    NewObj = 8,

    /// <summary>Pops a handle id, reads its field at the following 4-byte index, pushes that field's handle id (or -1 for none).</summary>
    LoadField = 9,

    /// <summary>Pops a value handle id then a target handle id, and stores the value into the target's field at the following 4-byte index.</summary>
    StoreField = 10,

    /// <summary>Pushes a copy of the top of stack without popping it.</summary>
    Dup = 11,

    /// <summary>Unconditionally jumps to the following 4-byte absolute address.</summary>
    Jump = 12,

    /// <summary>Pops a value; jumps to the following 4-byte absolute address if it was zero, otherwise continues past the operand.</summary>
    JumpIfZero = 13,

    /// <summary>Pops two values (b, a in pop order), pushes 1 if a &lt; b else 0.</summary>
    Lt = 14,

    /// <summary>Pops two values (b, a in pop order), pushes 1 if a &gt; b else 0.</summary>
    Gt = 15,

    /// <summary>Pops two values, pushes 1 if they're equal else 0.</summary>
    Eq = 16,

    /// <summary>Pops two values (b, a in pop order), pushes 1 if a &lt;= b else 0.</summary>
    Le = 17,

    /// <summary>Pops two values (b, a in pop order), pushes 1 if a &gt;= b else 0.</summary>
    Ge = 18,

    /// <summary>Pushes a copy of the value at the following 4-byte absolute stack index (0 = the bottom of the stack). Fails if the index is out of range.</summary>
    LoadLocal = 19,
}
