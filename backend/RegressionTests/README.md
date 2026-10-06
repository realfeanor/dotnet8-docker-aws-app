# Backend tests

This is an xUnit test project, not an application layer. It replaces the previous console regression runner. Run it from the repository root:

```bash
dotnet test backend/Dotnet8-Backend.sln
```

No Docker containers or SQL Server instance are needed. Registration persistence tests use a fresh in-memory SQLite database per test; they do not verify SQL Server retry or concurrency behavior. The API Dockerfile restores and publishes only the API and its dependencies; test packages are not dependencies of the API.

## Read your first test

Start with `AuthManagerTests.Login_WithWrongPassword_RejectsLoginAndPreservesPassword`. It follows three steps:

1. **Arrange:** create a user and a manager with a fake dependency.
2. **Act:** call `Login` with an incorrect password.
3. **Assert:** verify login failed and the stored password still works.

`[Fact]` marks a test with one case. `[Theory]` runs the same test with each set of `[InlineData]` values. `Assert.False`, `Assert.Equal`, and `Assert.Throws` express expected behavior. xUnit reports each failing case separately and continues running the others.

The test name describes the method, the situation, and the expected outcome. Each test creates its own state; it does not depend on another test running first. To add a test, create another public method with `[Fact]`, arrange the inputs, call the behavior, and assert the observable result.

## What the files test

| File | Purpose |
| --- | --- |
| `AuthManagerTests.cs` | Login business rules and preserving stored credentials |
| `ProductManagerTests.cs` | Product creation, duplicate names, category checks, updates, and deletion |
| `CategoryManagerTests.cs` | Protecting referenced categories and preserving identity during updates |
| `ControllerTests.cs` | DTO mapping, ignoring client-supplied creation IDs, and missing-product responses |
| `AspectTests.cs` | Authorization, validation, and cache invalidation through real Castle proxies |
| `RegistrationTests.cs` | Registration permissions, JWT claims, and rollback using an isolated in-memory SQLite database |
| `ModelTests.cs` | EF relationships, generated IDs, unique indexes, and migration SQL |
| `Fakes.cs` | In-memory substitutes for repositories and the user service |

Directly constructing a manager tests its business logic. It does **not** execute its AOP attributes; the aspect tests create real proxies to exercise those behaviors. Those tests share an xUnit fixture and run in a non-parallel collection because `ServiceTool` and log4net hold process-wide state. Other tests remain independent.

Model tests inspect EF metadata and generate SQL without opening a connection. Controller tests call controller methods directly; they do not exercise HTTP routing or ASP.NET middleware. Live database and HTTP behavior require separate integration tests.

## Run a subset

```bash
dotnet test backend/RegressionTests/RegressionTests.csproj --filter FullyQualifiedName~AuthManagerTests
dotnet test backend/RegressionTests/RegressionTests.csproj --filter Category=Integration
dotnet test backend/RegressionTests/RegressionTests.csproj --filter Category=Model
```

You can also run individual tests in Visual Studio's Test Explorer. For xUnit concepts and runner setup, see the [official getting-started guide](https://xunit.net/docs/getting-started/v2/getting-started).
