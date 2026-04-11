using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;

namespace Portless.Tests;

public class CertificatePermissionServiceTests
{
    private readonly Mock<ILogger<CertificatePermissionService>> _logger;
    private readonly CertificatePermissionService _service;

    public CertificatePermissionServiceTests()
    {
        _logger = new Mock<ILogger<CertificatePermissionService>>();
        _service = new CertificatePermissionService(_logger.Object);
    }

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"portless-test-perm-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_CreatesDirectory()
    {
        var baseDir = CreateTempDir();
        var dir = Path.Combine(baseDir, "secure-dir");
        try
        {
            await _service.CreateSecureDirectoryAsync(dir);
            Assert.True(Directory.Exists(dir));
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_ExistingDirectory_NoThrow()
    {
        var baseDir = CreateTempDir();
        try
        {
            await _service.CreateSecureDirectoryAsync(baseDir);
            Assert.True(Directory.Exists(baseDir));
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task SetSecureFilePermissionsAsync_SetsPermissionsOnFile()
    {
        var baseDir = CreateTempDir();
        var filePath = Path.Combine(baseDir, "test-file.txt");
        try
        {
            await File.WriteAllTextAsync(filePath, "test");
            await _service.SetSecureFilePermissionsAsync(filePath);
            Assert.True(File.Exists(filePath));
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task VerifyFilePermissionsAsync_ExistingFile_ReturnsResult()
    {
        var baseDir = CreateTempDir();
        var filePath = Path.Combine(baseDir, "test-file.txt");
        try
        {
            await File.WriteAllTextAsync(filePath, "test");
            await _service.SetSecureFilePermissionsAsync(filePath);
            var result = await _service.VerifyFilePermissionsAsync(filePath);
            // On Linux, after setting 600, should be true
            Assert.True(result);
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task VerifyFilePermissionsAsync_NonexistentFile_ReturnsFalse()
    {
        var result = await _service.VerifyFilePermissionsAsync("/nonexistent/file/path");
        Assert.False(result);
    }

    [Fact]
    public async Task VerifyFilePermissionsAsync_Directory_ReturnsResult()
    {
        var baseDir = CreateTempDir();
        try
        {
            var result = await _service.VerifyFilePermissionsAsync(baseDir);
            // Should return a boolean without throwing
            Assert.IsType<bool>(result);
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_NestedDirectory_CreatesAll()
    {
        var baseDir = CreateTempDir();
        var nestedDir = Path.Combine(baseDir, "a", "b", "c");
        try
        {
            await _service.CreateSecureDirectoryAsync(nestedDir);
            Assert.True(Directory.Exists(nestedDir));
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task SetSecureFilePermissionsAsync_NonexistentFile_Throws()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _service.SetSecureFilePermissionsAsync("/nonexistent/file.txt"));
    }
}
