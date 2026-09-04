# Klexir.Runtime

[![CI](https://github.com/Danny4897/Klexir.Runtime/actions/workflows/ci.yml/badge.svg)](https://github.com/Danny4897/Klexir.Runtime/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Docs](https://img.shields.io/badge/docs-vitepress-7c3aed.svg)](https://danny4897.github.io/Klexir.Runtime/)

A small stack-based bytecode VM, built to make explicit what's usually hidden under .NET: the interpretation loop, a call stack, an object heap with a real garbage collector. **Not a replacement for .NET in production** — this is the "how does a runtime actually work" repo.

> **Status: public research repo, not yet published to NuGet.** No language compiles to this yet — `Klexir.Lang` is a front end only so far, though `CallIndirect` closes the calling-convention gap a compiler targeting closures/recursion would have hit (see below). Reference the project directly until/unless it's published.

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
// A real loop: count down from 5 to 0 (backward Jump + Dup so JumpIfZero's test doesn't consume the counter)
var builder = BytecodeBuilder.New().Push(5);
var loopStart = builder.CurrentAddress;
builder.Dup();
var exitJump = builder.JumpIfZeroPlaceholder(); // patched below, once we know where the loop ends
builder.Push(1).Sub().Jump(loopStart);
builder.PatchInt32(exitJump, builder.CurrentAddress);
builder.Halt();

Result<long> countdown = new KlexirVm(builder.Build()).Run(); // Success(0)
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

```csharp
// A closure calling itself: factorial via CallIndirect, no fixed compile-time call target.
// body(n): if n < 2 then 1 else n * body(n - 1) — recurses through LoadLocal(1), its own closure handle.
var body = BytecodeBuilder.New();
body.LoadLocal(0).Push(2).Lt();
var elseJump = body.JumpIfZeroPlaceholder();
body.Push(1).Ret();
body.PatchInt32(elseJump, body.CurrentAddress);
body.LoadLocal(0)
    .LoadLocal(0).Push(1).Sub()
    .LoadLocal(1)          // the closure's own handle — no upvalue capture needed to recurse
    .CallIndirect(1)
    .Mul()
    .Ret();

var main = BytecodeBuilder.New()
    .Push(5)
    .NewObj(1)             // a closure object: field 0 is the entry point
    .Dup().Push(0).StoreField(0)
    .CallIndirect(1)
    .Halt()
    .Build();

var code = body.Build().Concat(main).ToArray();
Result<long> factorial5 = new KlexirVm(code, entryPoint: body.Build().Length).Run(); // Success(120)
```

---

## What's in the box

| Capability | API | Notes |
|---|---|---|
| Arithmetic + control flow | `OpCode` (`Push`/`Add`/`Sub`/`Mul`/`Div`/`Lt`/`Gt`/`Eq`/`Le`/`Ge`/`Call`/`Ret`/`Dup`/`Jump`/`JumpIfZero`/`LoadLocal`/`CallIndirect`/`Halt`) | `Call`/`Ret` share the value stack with a separate return-address stack; `Jump`/`JumpIfZero` + `Dup` are enough to write a real loop; `Lt`/`Gt`/`Eq`/`Le`/`Ge` push `1`/`0` |
| Locals | `LoadLocal <index>` | Reads (doesn't remove) a slot relative to the active call frame's base (0 outside any `CallIndirect` — so this is unchanged for `Call`/top-level code) — a compiler can bind a `let`/parameter to "whatever slot it lands at" and read it back later without popping it |
| Closures | `OpCode.CallIndirect <argCount>` | Calls a *value* — a heap object built from ordinary `NewObj`/`StoreField` whose field 0 is a code address and fields 1.. are captured values — instead of a fixed compile-time target. Stack before: `argCount` arguments then the closure's handle on top (peeked, not popped); inside the call, `LoadLocal(argCount)` reads that same handle back, so a closure can read its own upvalues via `LoadField` or recurse by calling itself, with no separate self-capture needed. `Ret` truncates back to the frame's base before pushing the result, discarding every argument/local the call pushed |
| Assembler | `BytecodeBuilder` | Fluent — emits the exact byte layout `OpCode` documents. `JumpPlaceholder`/`JumpIfZeroPlaceholder` + `PatchInt32` support the classic forward-jump back-patching pattern (emit a zero operand, remember its position, overwrite it once the target address is known) |
| Interpreter | `KlexirVm.Run()` | Returns `Result<long>` — stack underflow, division by zero, unknown opcode, etc. all fail the result instead of throwing |
| Heap | `ManagedHeap`, `OpCode.NewObj/LoadField/StoreField` | Mark-sweep GC; frees unreachable objects *including reference cycles* |
| Scheduling | `CooperativeScheduler` | Round-robin over step functions — standalone, not yet reachable from bytecode |

`OpCode` lives in `Klexir.Runtime.Abstractions` — it's the wire format a future compiler (`Klexir.Lang`) targets. Changing a value there is a breaking change.

## Not there yet

- No yield opcode, so `CooperativeScheduler` can't pause bytecode execution mid-program
- No metadata, exceptions, or native interop
- The heap's mark-sweep tracer treats every field as a potential object reference; `CallIndirect`'s closures store a raw code address in field 0 using the same `HeapHandle`-shaped slot as a real reference, so a `Collect()` during a program that builds closures can, in principle, keep an unrelated object alive if its id happens to collide with a code address. Harmless for correctness (nothing dereferences that "reference"), but real precise/typed GC is still unbuilt.

## Requirements

.NET 8 SDK + [MonadicSharp](https://www.nuget.org/packages/MonadicSharp/) `Result<T>`.
