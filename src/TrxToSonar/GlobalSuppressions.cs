// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.

using System.Diagnostics.CodeAnalysis;

[assembly:
    SuppressMessage(
        "Design",
        "CA1515:Consider making public types internal",
        Justification = "Model classes must be public for XML serialization",
        Scope = "namespaceanddescendants",
        Target = "~N:TrxToSonar.Model.Sonar")]

[assembly:
    SuppressMessage(
        "Design",
        "CA1515:Consider making public types internal",
        Justification = "Model classes must be public for XML serialization",
        Scope = "namespaceanddescendants",
        Target = "~N:TrxToSonar.Model.Trx")]

[assembly:
    SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Catching general exceptions is required for robust logging",
        Scope = "namespaceanddescendants",
        Target = "~N:TrxToSonar")]
