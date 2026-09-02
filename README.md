# Klexir.Runtime

Experimental bytecode runtime for Klexir. Exists to make explicit what happens under .NET (interpretation loop, stack frames, memory model), not to replace .NET in production.

Only `Klexir.Runtime.Abstractions` is a public NuGet package: `OpCode`, the bytecode contract `Klexir.Lang` will compile against.

The first increment is a stack-based interpreter for arithmetic: `Push`/`Add`/`Sub`/`Mul`/`Div`/`Halt`. `BytecodeBuilder` assembles a program fluently (`BytecodeBuilder.New().Push(5).Push(3).Add().Halt().Build()`); `KlexirVm.Run()` executes it and returns a `Result<long>` — stack underflow, division by zero, a truncated operand, a missing `Halt`, or an unknown opcode all fail the result instead of throwing. The interpreter itself is deliberately low-level (raw byte decoding, a plain `long` stack), not a polished public API.

`Call`/`Ret` share the value stack and use a separate call stack of return addresses: `Call <addr>` pushes the address of the instruction after its own operand, then jumps; `Ret` pops that address and jumps back (failing if the call stack is empty). `KlexirVm` now takes an optional entry point, so a subroutine's bytes can sit anywhere in the program and execution starts wherever the caller specifies.

`ManagedHeap` is an experimental mark-sweep collector: `Allocate(fieldCount)` returns a `HeapHandle` to a fixed-arity object whose fields hold other handles (or `HeapHandle.None`); `AddRoot`/`RemoveRoot` maintain an explicit root set; `Collect()` traces reachability from the roots and frees everything else — including a reference cycle with no root pointing into it, which a naive refcounting scheme couldn't reclaim. Not yet wired into `KlexirVm` (no opcodes allocate or dereference heap objects yet) — that integration, plus a cooperative scheduler and metadata/exceptions/interop, follow in later increments.
