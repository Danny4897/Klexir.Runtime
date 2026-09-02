using MonadicSharp;

namespace Klexir.Runtime;

/// <summary>
/// Experimental heap: fixed-arity objects whose fields are <see cref="HeapHandle"/> references to other objects
/// (or <see cref="HeapHandle.None"/>). <see cref="Collect"/> is a classic mark-sweep collector — it traces from an
/// explicit root set, so unreachable objects are freed even if they only reference each other in a cycle.
/// </summary>
public sealed class ManagedHeap
{
    private readonly Dictionary<int, HeapHandle[]> _objects = [];
    private readonly HashSet<int> _roots = [];
    private int _nextId;

    public int ObjectCount => _objects.Count;

    public HeapHandle Allocate(int fieldCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fieldCount);

        var handle = new HeapHandle(_nextId++);
        var fields = new HeapHandle[fieldCount];
        Array.Fill(fields, HeapHandle.None);
        _objects[handle.Id] = fields;
        return handle;
    }

    public void AddRoot(HeapHandle handle) => _roots.Add(handle.Id);

    public void RemoveRoot(HeapHandle handle) => _roots.Remove(handle.Id);

    public bool IsAlive(HeapHandle handle) => _objects.ContainsKey(handle.Id);

    public Result<Unit> SetField(HeapHandle handle, int index, HeapHandle target)
    {
        var validated = ValidateField(handle, index);
        if (validated.IsFailure)
        {
            return Result<Unit>.Failure(validated.Error);
        }

        _objects[handle.Id][index] = target;
        return Result<Unit>.Success(Unit.Value);
    }

    public Result<HeapHandle> GetField(HeapHandle handle, int index)
    {
        var validated = ValidateField(handle, index);
        return validated.IsFailure
            ? Result<HeapHandle>.Failure(validated.Error)
            : Result<HeapHandle>.Success(_objects[handle.Id][index]);
    }

    /// <summary>Marks everything reachable from the root set, frees everything else, and returns the number of objects freed.</summary>
    public int Collect()
    {
        var reachable = new HashSet<int>();
        var pending = new Stack<int>(_roots);

        while (pending.Count > 0)
        {
            var id = pending.Pop();
            if (!reachable.Add(id) || !_objects.TryGetValue(id, out var fields))
            {
                continue;
            }

            foreach (var field in fields)
            {
                if (field != HeapHandle.None)
                {
                    pending.Push(field.Id);
                }
            }
        }

        var garbage = _objects.Keys.Where(id => !reachable.Contains(id)).ToList();
        foreach (var id in garbage)
        {
            _objects.Remove(id);
        }

        return garbage.Count;
    }

    private Result<Unit> ValidateField(HeapHandle handle, int index)
    {
        if (!_objects.TryGetValue(handle.Id, out var fields))
        {
            return Result<Unit>.Failure(Error.NotFound("HeapObject", handle.ToString()));
        }

        return index >= 0 && index < fields.Length
            ? Result<Unit>.Success(Unit.Value)
            : Result<Unit>.Failure(Error.Create($"Field index {index} is out of range for object {handle} (arity {fields.Length})."));
    }
}
