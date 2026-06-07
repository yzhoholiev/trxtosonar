# TRX to Sonar

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A .NET tool that converts TRX (Visual Studio Test Results) files to SonarQube's Generic Test Data format, enabling test coverage visualization in SonarQube.

[Forked from gregoryb/trxtosonar](https://github.com/gregoryb/trxtosonar)

## Features

- Converts TRX test result files to SonarQube Generic Execution format
- Recursively scans directories for TRX files
- Supports both relative and absolute file paths
- Distributed as a .NET global tool for easy installation
- Ships as a **Native AOT** precompiled tool on common platforms (fast startup, no JIT), with a portable fallback elsewhere

## Installation

### As a .NET Global Tool

Each [release](https://github.com/yzhoholiev/trxtosonar/releases) attaches the tool's
NuGet packages. Download them all into a single folder and install from there:

```bash
dotnet tool install -g Trx2Sonar --add-source <folder-with-downloaded-packages>
```

Keep every downloaded `.nupkg` in the same folder — the pointer package, the RID-specific
Native AOT packages (`win-x64`, `linux-x64`, `osx-arm64`), and the portable `any` fallback.
The .NET CLI automatically selects the self-contained Native AOT build for your platform,
or the portable build (which needs the .NET 10 runtime) on other platforms.

### From Source

```bash
git clone https://github.com/yzhoholiev/trxtosonar.git
cd trxtosonar
dotnet build
```

## Usage

### Command Line

```bash
dotnet-trx2sonar -d <solution-directory> -o <output-file> [options]
```

### Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--directory` | `-d` | Yes | Solution directory to parse (searches recursively for TRX files) |
| `--output` | `-o` | Yes | Output filename for the SonarQube Generic Test Data XML |
| `--absolute` | `-a` | No | Use absolute paths for file references in the output |
| `--verbosity` | | No | Log verbosity: `Quiet`, `Minimal`, `Normal` (default), `Detailed`, `Diagnostic` |
| `--help` | `-h`, `-?` | No | Display help information |

### Examples

**Basic usage with relative paths:**
```bash
dotnet-trx2sonar -d ./TestResults -o sonar-test-results.xml
```

**Using absolute paths:**
```bash
dotnet-trx2sonar -d C:\Projects\MyApp\TestResults -o C:\Reports\sonar-test-results.xml -a
```

**Quiet logs (useful for CI/CD):**
```bash
dotnet-trx2sonar -d ./TestResults -o sonar-test-results.xml --verbosity Quiet
```

## Integration with SonarQube

After generating the XML file, configure your SonarQube analysis to include it:

### Using sonar-project.properties

```properties
sonar.testExecutionReportPaths=sonar-test-results.xml
```

### Using command line

```bash
dotnet sonarscanner begin /k:"project-key" /d:sonar.testExecutionReportPaths="sonar-test-results.xml"
# ... build and test ...
dotnet sonarscanner end
```

## How It Works

1. Scans the specified directory recursively for `.trx` files
2. Parses each TRX file to extract test execution data
3. Converts the test results to SonarQube's Generic Test Data XML format
4. Outputs a single consolidated XML file that can be imported by SonarQube

## Requirements

- .NET 10.0 SDK or later to install via `dotnet tool install`. The Native AOT builds run
  as self-contained native binaries (no runtime needed to execute); the portable fallback
  requires the .NET 10 runtime.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Original project by [gregoryb](https://github.com/gregoryb/trxtosonar)
- Maintained by [Yurii Zhoholiev](https://github.com/yzhoholiev)
