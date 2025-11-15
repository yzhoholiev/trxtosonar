using System.Diagnostics.CodeAnalysis;

namespace TrxToSonar;

[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Using modern primary constructor pattern")]
[SuppressMessage("Major Code Smell", "S3871:Exception types should be \"public\"", Justification = "Internal exception by design")]
internal sealed class TrxToSonarException(string? message = null, Exception? innerException = null)
    : Exception(message, innerException);
