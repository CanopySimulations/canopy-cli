# Canopy.Cli.Shared.Tests

Test project for `Canopy.Cli.Shared`.

## Structure

This test project follows the same directory structure as the main project for easy navigation:

```
Canopy.Cli.Shared.Tests/
  StudyProcessing/
    ChannelData/
      TryGetVectorResultsDomainTests.cs
      TelemetryChannelSerializerTests.cs
      DomainChannelFilesTests.cs
  Canopy.Cli.Shared.Tests.csproj
```

## Running Tests

### Visual Studio
- Open Test Explorer (Test > Test Explorer)
- Click "Run All" or run individual tests

### Command Line
```bash
cd Canopy.Cli.Shared.Tests
dotnet test
```

## Dependencies

- **MSTest**: Test framework (consistent with the main canopy-api solution)
- **Parquet.Net**: For creating test Parquet files
- **Canopy.Cli.Shared**: Project under test

## Adding New Tests

When adding tests for new classes:
1. Mirror the directory structure from `Canopy.Cli.Shared`
2. Name test classes with `Tests` suffix (e.g., `MyClassTests`)
3. Use `[TestClass]` and `[TestMethod]` attributes
4. Follow naming convention: `MethodName_WithCondition_ShouldExpectedBehavior`
