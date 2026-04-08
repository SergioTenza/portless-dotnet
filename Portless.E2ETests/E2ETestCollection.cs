using Xunit;

namespace Portless.E2ETests;

/// <summary>
/// xUnit collection definition for E2E tests.
/// Prevents parallel execution of E2E tests since they share the proxy process.
/// </summary>
[CollectionDefinition("E2E", DisableParallelization = true)]
public class E2ETestCollection : ICollectionFixture<E2ETestFixture>
{
    // This class is never instantiated. It just defines the collection.
}
