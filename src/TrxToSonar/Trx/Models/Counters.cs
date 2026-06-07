namespace TrxToSonar.Trx.Models;

public sealed record Counters
{
    public int Total { get; init; }

    public int Executed { get; init; }

    public int Passed { get; init; }

    public int Failed { get; init; }

    public int Error { get; init; }

    public int Timeout { get; init; }

    public int Aborted { get; init; }

    public int Inconclusive { get; init; }

    public int PassedButRunAborted { get; init; }

    public int NotRunnable { get; init; }

    public int NotExecuted { get; init; }

    public int Disconnected { get; init; }

    public int Warning { get; init; }

    public int Completed { get; init; }

    public int InProgress { get; init; }

    public int Pending { get; init; }
}
