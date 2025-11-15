using TrxToSonar.Model.Sonar;

namespace TrxToSonar;

internal interface IConverter
{
    SonarDocument? Parse(string? solutionDirectory, bool useAbsolutePath);
    bool Save(SonarDocument sonarDocument, string outputFilename);
}
