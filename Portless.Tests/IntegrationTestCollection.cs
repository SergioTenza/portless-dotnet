namespace Portless.Tests;

/// <summary>
/// Integration tests share process-wide resources (PORTLESS_STATE_DIR env var,
/// file-based route store locks). This collection disables parallelization
/// to prevent lock timeout failures when tests run concurrently.
/// </summary>
[CollectionDefinition("Integration Tests", DisableParallelization = true)]
public class IntegrationTestCollection { }
