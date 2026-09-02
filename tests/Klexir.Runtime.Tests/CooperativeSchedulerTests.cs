using FluentAssertions;
using Xunit;

namespace Klexir.Runtime.Tests;

public sealed class CooperativeSchedulerTests
{
    [Fact]
    public void RunToCompletion_interleaves_scheduled_tasks_round_robin()
    {
        var order = new List<string>();
        var scheduler = new CooperativeScheduler();
        var stepsA = 0;
        var stepsB = 0;

        scheduler.Schedule(() =>
        {
            order.Add("A");
            stepsA++;
            return stepsA < 3;
        });
        scheduler.Schedule(() =>
        {
            order.Add("B");
            stepsB++;
            return stepsB < 3;
        });

        scheduler.RunToCompletion();

        order.Should().Equal("A", "B", "A", "B", "A", "B");
    }

    [Fact]
    public void RunToCompletion_returns_the_total_number_of_steps_executed()
    {
        var scheduler = new CooperativeScheduler();
        var remaining = 4;
        scheduler.Schedule(() => --remaining > 0);

        var totalSteps = scheduler.RunToCompletion();

        totalSteps.Should().Be(4);
    }

    [Fact]
    public void A_task_that_finishes_early_stops_being_scheduled_while_others_continue()
    {
        var order = new List<string>();
        var scheduler = new CooperativeScheduler();
        var stepsA = 0;
        var stepsB = 0;

        scheduler.Schedule(() =>
        {
            order.Add("A");
            stepsA++;
            return stepsA < 1; // finishes after its first step
        });
        scheduler.Schedule(() =>
        {
            order.Add("B");
            stepsB++;
            return stepsB < 3;
        });

        scheduler.RunToCompletion();

        order.Should().Equal("A", "B", "B", "B");
    }

    [Fact]
    public void RunToCompletion_with_no_scheduled_tasks_returns_zero()
    {
        var scheduler = new CooperativeScheduler();

        scheduler.RunToCompletion().Should().Be(0);
    }
}
