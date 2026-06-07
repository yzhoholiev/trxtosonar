namespace TrxToSonar.Trx.Models;

public enum Outcome
{
    Error,

    Failed,

    Timeout,

    Aborted,

    Inconclusive,

    PassedButRunAborted,

    NotRunnable,

    NotExecuted,

    Disconnected,

    Warning,

    Passed,

    Completed,

    InProgress,

    Pending
}
