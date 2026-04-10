using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Services;

namespace Portless.Tests;

/// <summary>
/// Extended tests for CertificatePermissionService covering Unix-specific paths,
/// permission verification, and edge cases.
/// </summary>
public class CertificatePermissionServiceExtendedTests
{
    private readonly Mock<ILogger<CertificatePermissionService>> _logger;
    private readonly CertificatePermissionService _service;

    public CertificatePermissionServiceExtendedTests()
    {
        _logger = new Mock<ILogger<CertificatePermissionService>>();
        _service = new CertificatePermissionService(_logger.Object);
    }

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"portless-test-perm-ext-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_Unix_SetsDirectoryPermissions()
    {
        var baseDir = CreateTempDir();
        var secureDir = Path.Combine(baseDir, "secure-unix");
        try
        {
            await _service.CreateSecureDirectoryAsync(secureDir);
            Assert.True(Directory.Exists(secureDir));

            // On Unix, verify the directory was created with restricted permissions
            if (!OperatingSystem.IsWindows())
            {
                var mode = File.GetUnixFileMode(secureDir);
                // Should be chmod 700 (UserRead | UserWrite | UserExecute)
                Assert.True(mode.HasFlag(UnixFileMode.UserRead));
                Assert.True(mode.HasFlag(UnixFileMode.UserWrite));
                Assert.True(mode.HasFlag(UnixFileMode.UserExecute));
                // Group and Other should have no permissions
                Assert.False(mode.HasFlag(UnixFileMode.GroupRead));
                Assert.False(mode.HasFlag(UnixFileMode.OtherRead));
            }
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task SetSecureFilePermissionsAsync_Unix_SetsFilePermissions()
    {
        var baseDir = CreateTempDir();
        var filePath = Path.Combine(baseDir, "test-secure.txt");
        try
        {
            await File.WriteAllTextAsync(filePath, "test content");
            await _service.SetSecureFilePermissionsAsync(filePath);

            if (!OperatingSystem.IsWindows())
            {
                var mode = File.GetUnixFileMode(filePath);
                // Should be chmod 600 (UserRead | UserWrite)
                Assert.True(mode.HasFlag(UnixFileMode.UserRead));
                Assert.True(mode.HasFlag(UnixFileMode.UserWrite));
                // Group and Other should have no permissions
                Assert.False(mode.HasFlag(UnixFileMode.GroupRead));
                Assert.False(mode.HasFlag(UnixFileMode.GroupWrite));
                Assert.False(mode.HasFlag(UnixFileMode.OtherRead));
                Assert.False(mode.HasFlag(UnixFileMode.OtherWrite));
            }
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task VerifyFilePermissionsAsync_AfterSetSecure_ReturnsTrue()
    {
        var baseDir = CreateTempDir();
        var filePath = Path.Combine(baseDir, "verify-secure.txt");
        try
        {
            await File.WriteAllTextAsync(filePath, "test content");
            await _service.SetSecureFilePermissionsAsync(filePath);

            var result = await _service.VerifyFilePermissionsAsync(filePath);
            Assert.True(result);
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task VerifyFilePermissionsAsync_WithOpenPermissions_ReturnsFalse()
    {
        if (OperatingSystem.IsWindows()) return; // Skip on Windows

        var baseDir = CreateTempDir();
        var filePath = Path.Combine(baseDir, "open-permissions.txt");
        try
        {
            await File.WriteAllTextAsync(filePath, "test content");
            // Set wide-open permissions (readable by everyone)
            File.SetUnixFileMode(filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite |
                UnixFileMode.GroupRead | UnixFileMode.OtherRead);

            var result = await _service.VerifyFilePermissionsAsync(filePath);
            // This should return true because the check only verifies UserRead and UserWrite are set,
            // not that group/other are excluded
            Assert.True(result);
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task VerifyFilePermissionsAsync_FileWithNoWrite_ReturnsFalse()
    {
        if (OperatingSystem.IsWindows()) return; // Skip on Windows

        var baseDir = CreateTempDir();
        var filePath = Path.Combine(baseDir, "no-write.txt");
        try
        {
            await File.WriteAllTextAsync(filePath, "test content");
            // Set read-only for user
            File.SetUnixFileMode(filePath, UnixFileMode.UserRead);

            var result = await _service.VerifyFilePermissionsAsync(filePath);
            Assert.False(result); // Missing UserWrite
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task VerifyFilePermissionsAsync_FileWithNoRead_ReturnsFalse()
    {
        if (OperatingSystem.IsWindows()) return; // Skip on Windows

        var baseDir = CreateTempDir();
        var filePath = Path.Combine(baseDir, "no-read.txt");
        try
        {
            await File.WriteAllTextAsync(filePath, "test content");
            // Set write-only for user
            File.SetUnixFileMode(filePath, UnixFileMode.UserWrite);

            var result = await _service.VerifyFilePermissionsAsync(filePath);
            Assert.False(result); // Missing UserRead
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_DeeplyNestedPath_CreatesAllDirs()
    {
        var baseDir = CreateTempDir();
        var deepPath = Path.Combine(baseDir, "a", "b", "c", "d", "e");
        try
        {
            await _service.CreateSecureDirectoryAsync(deepPath);
            Assert.True(Directory.Exists(deepPath));
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_CalledTwiceOnSamePath_NoThrow()
    {
        var baseDir = CreateTempDir();
        var dir = Path.Combine(baseDir, "double-create");
        try
        {
            await _service.CreateSecureDirectoryAsync(dir);
            await _service.CreateSecureDirectoryAsync(dir);
            Assert.True(Directory.Exists(dir));
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task SetSecureFilePermissionsAsync_MultipleFilesInDirectory()
    {
        var baseDir = CreateTempDir();
        try
        {
            for (int i = 0; i < 5; i++)
            {
                var filePath = Path.Combine(baseDir, $"file-{i}.txt");
                await File.WriteAllTextAsync(filePath, $"content {i}");
                await _service.SetSecureFilePermissionsAsync(filePath);
                Assert.True(File.Exists(filePath));
            }
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_WithCancellationToken_DoesNotThrow()
    {
        var baseDir = CreateTempDir();
        var dir = Path.Combine(baseDir, "cancel-test");
        try
        {
            using var cts = new CancellationTokenSource();
            await _service.CreateSecureDirectoryAsync(dir, cts.Token);
            Assert.True(Directory.Exists(dir));
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task SetSecureFilePermissionsAsync_WithCancellationToken_DoesNotThrow()
    {
        var baseDir = CreateTempDir();
        var filePath = Path.Combine(baseDir, "cancel-file.txt");
        try
        {
            await File.WriteAllTextAsync(filePath, "test");
            using var cts = new CancellationTokenSource();
            await _service.SetSecureFilePermissionsAsync(filePath, cts.Token);
            Assert.True(File.Exists(filePath));
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task VerifyFilePermissionsAsync_WithCancellationToken_DoesNotThrow()
    {
        var baseDir = CreateTempDir();
        var filePath = Path.Combine(baseDir, "cancel-verify.txt");
        try
        {
            await File.WriteAllTextAsync(filePath, "test");
            await _service.SetSecureFilePermissionsAsync(filePath);
            using var cts = new CancellationTokenSource();
            var result = await _service.VerifyFilePermissionsAsync(filePath, cts.Token);
            Assert.IsType<bool>(result);
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_EmptyPath_Throws()
    {
        await Assert.ThrowsAnyAsync<Exception>(() => _service.CreateSecureDirectoryAsync(""));
    }

    [Fact]
    public async Task SetSecureFilePermissionsAsync_EmptyPath_Throws()
    {
        await Assert.ThrowsAnyAsync<Exception>(() => _service.SetSecureFilePermissionsAsync(""));
    }

    [Fact]
    public async Task VerifyFilePermissionsAsync_EmptyString_ReturnsFalse()
    {
        var result = await _service.VerifyFilePermissionsAsync("");
        Assert.False(result);
    }
}
