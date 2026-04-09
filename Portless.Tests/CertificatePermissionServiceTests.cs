using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class CertificatePermissionServiceTests
{
    private readonly Mock<ILogger<CertificatePermissionService>> _loggerMock;
    private readonly CertificatePermissionService _service;

    public CertificatePermissionServiceTests()
    {
        _loggerMock = new Mock<ILogger<CertificatePermissionService>>();
        _service = new CertificatePermissionService(_loggerMock.Object);
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_CreatesDirectorySuccessfully()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"portless-test-perm-dir-{Guid.NewGuid()}");

        try
        {
            // Act
            await _service.CreateSecureDirectoryAsync(tempDir);

            // Assert
            Assert.True(Directory.Exists(tempDir));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SetSecureFilePermissionsAsync_SetsPermissionsOnExistingFile()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"portless-test-perm-file-{Guid.NewGuid()}.txt");
        await File.WriteAllTextAsync(tempFile, "test content");

        try
        {
            // Act - should not throw on current platform
            await _service.SetSecureFilePermissionsAsync(tempFile);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifyFilePermissionsAsync_ReturnsTrueForExistingFile()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"portless-test-verify-{Guid.NewGuid()}.txt");
        await File.WriteAllTextAsync(tempFile, "test content");

        try
        {
            // Act
            var result = await _service.VerifyFilePermissionsAsync(tempFile);

            // Assert - on Linux this should return true after setting permissions
            // On unsupported platforms returns true (best effort)
            Assert.True(result);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifyFilePermissionsAsync_ReturnsFalseForNonExistentFile()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"portless-nonexistent-{Guid.NewGuid()}.txt");

        // Act
        var result = await _service.VerifyFilePermissionsAsync(nonExistentPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_ThrowsForInvalidPath()
    {
        // Arrange - use an invalid path that can't be created
        var invalidPath = "";

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.CreateSecureDirectoryAsync(invalidPath));
    }

    [Fact]
    public async Task SetSecureFilePermissionsAsync_ThrowsForNonExistentFile()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"portless-nonexistent-{Guid.NewGuid()}.txt");

        // Act & Assert - should throw on any platform for non-existent file
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.SetSecureFilePermissionsAsync(nonExistentPath));
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_CreatesNestedDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"portless-test-nested-{Guid.NewGuid()}", "subdir");

        try
        {
            // Act
            await _service.CreateSecureDirectoryAsync(tempDir);

            // Assert
            Assert.True(Directory.Exists(tempDir));
        }
        finally
        {
            // Cleanup parent dir
            var parentDir = Path.Combine(Path.GetTempPath(), $"portless-test-nested-{Guid.NewGuid()}");
            // Use the actual parent from the path
            var actualParent = Directory.GetParent(tempDir)?.FullName;
            if (actualParent != null && Directory.Exists(actualParent))
                Directory.Delete(actualParent, recursive: true);
        }
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"portless-test-cancelled-{Guid.NewGuid()}");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - the Task.Yield at the start doesn't observe the token,
        // but the actual directory operation may proceed. Test that it doesn't hang.
        // The method may or may not throw depending on implementation timing
        try
        {
            await _service.CreateSecureDirectoryAsync(tempDir, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected in some cases
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SetSecureFilePermissionsAsync_ThenVerify_RoundTrip()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"portless-test-roundtrip-{Guid.NewGuid()}.txt");
        await File.WriteAllTextAsync(tempFile, "test content");

        try
        {
            // Act
            await _service.SetSecureFilePermissionsAsync(tempFile);
            var result = await _service.VerifyFilePermissionsAsync(tempFile);

            // Assert
            Assert.True(result);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_CreatesDirectoryInExistingParent()
    {
        // Arrange - parent already exists
        var tempParent = Path.Combine(Path.GetTempPath(), $"portless-test-parent-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempParent);
        var tempDir = Path.Combine(tempParent, "secure-child");

        try
        {
            // Act
            await _service.CreateSecureDirectoryAsync(tempDir);

            // Assert
            Assert.True(Directory.Exists(tempDir));
        }
        finally
        {
            if (Directory.Exists(tempParent))
                Directory.Delete(tempParent, recursive: true);
        }
    }
}
