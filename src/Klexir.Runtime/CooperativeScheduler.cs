namespace Klexir.Runtime;

/// <summary>
/// Cooperative round-robin scheduler: each scheduled task is a step function run to completion of one unit of
/// work, returning whether it has more work left. Not yet wired into <see cref="KlexirVm"/> — that needs a yield
/// opcode so a running program can voluntarily hand control back, which is a later increment.
/// </summary>
public sealed class CooperativeScheduler
{
    private readonly Queue<Func<bool>> _tasks = new();

    public void Schedule(Func<bool> step) => _tasks.Enqueue(step);

    /// <summary>Steps every scheduled task once, re-queuing any that report more work, until none remain. Returns the total number of steps executed.</summary>
    public int RunToCompletion()
    {
        var totalSteps = 0;

        while (_tasks.TryDequeue(out var task))
        {
            totalSteps++;
            if (task())
            {
                _tasks.Enqueue(task);
            }
        }

        return totalSteps;
    }
}
