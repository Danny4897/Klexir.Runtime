# Klexir.Runtime

[![CI](https://github.com/Danny4897/Klexir.Runtime/actions/workflows/ci.yml/badge.svg)](https://github.com/Danny4897/Klexir.Runtime/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

A small stack-based bytecode VM, built to make explicit what's usually hidden under .NET: the interpretation loop, a call stack, an object heap with a real garbage collector. **Not a replacement for .NET in production** — this is the "how does a runtime actually work" repo.

> **Status: private research repo, not published to NuGet.** No language compiles to this yet — `Klexir.Lang` is a front end only so far (see below). Reference the project directly until/unless it's published.

---

## Quick example — assemble and run bytecode by hand

```csharp
// (5 + 3) * 2
var code = BytecodeBuilder.New()
    .Push(5).Push(3).Add()
    .Push(2).Mul()
    .Halt()
    .Build();

Result<long> result = new KlexirVm(code).Run(); // Success(16)
```

```csharp
// A tiny heap-allocated linked object: obj.field0 = childHandle
var code = BytecodeBuilder.New()
    .NewObj(1)        // allocate a 1-field object → its handle id is on the stack
    .NewObj(0)         // allocate a second object
    .StoreField(0)     // first.field0 = second
    .Halt()
    .Build();

var vm = new KlexirVm(code);
vm.Run();
vm.Heap.Collect(); // mark-sweep — nothing is rooted, so both objects are freed
```

---

## What's in the box

| Capability | API | Notes |
|---|---|---|
| Arithmetic + control flow | `OpCode` (`Push`/`Add`/`Sub`/`Mul`/`Div`/`Call`/`Ret`/`Halt`) | `Call`/`Ret` share the value stack with a separate return-address stack |
| Assembler | `BytecodeBuilder` | Fluent — emits the exact byte layout `OpCode` documents |
| Interpreter | `KlexirVm.Run()` | Returns `Result<long>` — stack underflow, division by zero, unknown opcode, etc. all fail the result instead of throwing |
| Heap | `ManagedHeap`, `OpCode.NewObj/LoadField/StoreField` | Mark-sweep GC; frees unreachable objects *including reference cycles* |
| Scheduling | `CooperativeScheduler` | Round-robin over step functions — standalone, not yet reachable from bytecode |

`OpCode` lives in `Klexir.Runtime.Abstractions` — it's the wire format a future compiler (`Klexir.Lang`) targets. Changing a value there is a breaking change.

## Not there yet

- No yield opcode, so `CooperativeScheduler` can't pause bytecode execution mid-program
- No `Jump`/branch opcodes — every `if` in a source language would need to compile to something else (or this gets added)
- No local-variable opcodes (`Push`/`Pop`/arithmetic operate purely on the value stack) — a real compiler targeting this needs stack-slot accounting or new opcodes
- No metadata, exceptions, or native interop

## Requirements

.NET 8 SDK + [MonadicSharp](https://www.nuget.org/packages/MonadicSharp/) `Result<T>`.
