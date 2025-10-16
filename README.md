# TRX to Sonar

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A .NET tool that converts TRX (Visual Studio Test Results) files to SonarQube's Generic Test Data format, enabling test coverage visualization in SonarQube.

[Forked from gregoryb/trxtosonar](https://github.com/gregoryb/trxtosonar)

## Features

- 🔄 Converts TRX test result files to SonarQube Generic Execution format
- 📁 Recursively scans directories for TRX files
- 🛣️ Supports both relative and absolute file paths
- 🚀 Distributed as a .NET global tool for easy installation
- 🎨 Clean console output with optional logo suppression

## Installation

### As a .NET Global Tool

```bash
dotnet tool install -g Trx2Sonar
```

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
| `--no-logo` | | No | Suppress logo and version information at startup |
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

**Suppress logo (useful for CI/CD):**
```bash
dotnet-trx2sonar -d ./TestResults -o sonar-test-results.xml --no-logo
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

- .NET 8.0 or later

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Original project by [gregoryb](https://github.com/gregoryb/trxtosonar)
- Maintained by [Yurii Zhoholiev](https://github.com/yzhoholiev)

## TODO

- Add more comprehensive tests
- Support for additional test result formats
