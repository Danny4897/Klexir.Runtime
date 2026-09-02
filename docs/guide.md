# Quick example — assemble and run bytecode by hand

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

See the [full README](https://github.com/Danny4897/Klexir.Runtime#readme) on GitHub for object-heap allocation, garbage collection, and the current gaps (still no language compiles to this bytecode — `Klexir.Lang` is a front end only so far).
