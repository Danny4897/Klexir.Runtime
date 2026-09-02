# Klexir.Runtime

Experimental bytecode runtime for Klexir. Exists to make explicit what happens under .NET (interpretation loop, stack frames, memory model), not to replace .NET in production.

Only `Klexir.Runtime.Abstractions` is a public NuGet package: `OpCode`, the bytecode contract `Klexir.Lang` will compile against.

**Interpreter.** A stack-based VM for arithmetic and control flow: `Push`/`Add`/`Sub`/`Mul`/`Div`/`Halt`, plus `Call`/`Ret` sharing the value stack with a separate call stack of return addresses (`Call <addr>` pushes the address past its own operand, then jumps; `Ret` pops it and jumps back, failing if the call stack is empty). `BytecodeBuilder` assembles a program fluently (`BytecodeBuilder.New().Push(5).Push(3).Add().Halt().Build()`); `KlexirVm.Run()` (with an optional entry point, so a subroutine's bytes can sit anywhere in the program) executes it and returns a `Result<long>` — stack underflow, division by zero, a truncated operand, a missing `Halt`, an unknown opcode, or a return with no active call all fail the result instead of throwing. Deliberately low-level (raw byte decoding, a plain `long` stack), not a polished public API.

**Heap.** `ManagedHeap` is an experimental mark-sweep collector: `Allocate(fieldCount)` returns a `HeapHandle` to a fixed-arity object whose fields hold other handles (or `HeapHandle.None`); `AddRoot`/`RemoveRoot` maintain an explicit root set; `Collect()` traces reachability from the roots and frees everything else — including a reference cycle with no root pointing into it, which a naive refcounting scheme couldn't reclaim. `KlexirVm` owns one via `NewObj`/`LoadField`/`StoreField` opcodes (handle ids travel on the ordinary `long` value stack) and exposes it as `Heap` so a host can manage roots and call `Collect()` around `Run()` — the interpreter never collects on its own.

**Scheduling.** `CooperativeScheduler` runs scheduled step functions round-robin, re-queuing any that report more work, until none remain. Not yet wired into `KlexirVm` — that needs a yield opcode so a running program can voluntarily hand control back, a later increment.

Still open: metadata, exceptions, and interop.
