using System.Diagnostics.CodeAnalysis;

[assembly:
    SuppressMessage(
        "Design",
        "CA1515:Consider making public types internal",
        Justification = "Public data model of the SonarQube generic-test XML schema",
        Scope = "namespaceanddescendants",
        Target = "~N:TrxToSonar.Sonar.Models")]

[assembly:
    SuppressMessage(
        "Design",
        "CA1515:Consider making public types internal",
        Justification = "Public data model of the TRX XML schema",
        Scope = "namespaceanddescendants",
        Target = "~N:TrxToSonar.Trx.Models")]

[assembly:
    SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Catching general exceptions is required for robust logging",
        Scope = "namespaceanddescendants",
        Target = "~N:TrxToSonar")]
