# Klexir.Runtime

Experimental bytecode runtime for Klexir. Exists to make explicit what happens under .NET (interpretation loop, stack frames, memory model), not to replace .NET in production.

Only `Klexir.Runtime.Abstractions` is a public NuGet package: `OpCode`, the bytecode contract `Klexir.Lang` will compile against.

The first increment is a stack-based interpreter for arithmetic: `Push`/`Add`/`Sub`/`Mul`/`Div`/`Halt`. `BytecodeBuilder` assembles a program fluently (`BytecodeBuilder.New().Push(5).Push(3).Add().Halt().Build()`); `KlexirVm.Run()` executes it and returns a `Result<long>` — stack underflow, division by zero, a truncated operand, a missing `Halt`, or an unknown opcode all fail the result instead of throwing. The interpreter itself is deliberately low-level (raw byte decoding, a plain `long` stack), not a polished public API.

`Call`/`Ret` (and the frame model they need), the object/memory model, a cooperative scheduler, and metadata/exceptions/interop follow in later increments.
