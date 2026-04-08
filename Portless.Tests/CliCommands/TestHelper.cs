using Spectre.Console.Cli;

namespace Portless.Tests.CliCommands;

/// <summary>
/// Stub implementation of IRemainingArguments for unit tests.
/// </summary>
internal sealed class TestRemainingArguments : IRemainingArguments
{
    public IReadOnlyList<string> Raw { get; } = [];
    public ILookup<string, string> Parsed { get; } = new Dictionary<string, string>().ToLookup(kv => kv.Key, kv => kv.Value);
}
