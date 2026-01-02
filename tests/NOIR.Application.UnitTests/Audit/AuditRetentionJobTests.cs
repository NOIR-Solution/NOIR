using System.Reflection;
using NOIR.Infrastructure.Audit;

namespace NOIR.Application.UnitTests.Audit;

/// <summary>
/// Unit tests for AuditRetentionJob.
/// Tests configuration validation, transient error detection, and job execution logic.
/// </summary>
public class AuditRetentionJobTests
{
    private readonly Mock<ApplicationDbContext> _dbContextMock;
    private readonly Mock<IFileStorage> _fileStorageMock;
    private readonly Mock<ILogger<AuditRetentionJob>> _loggerMock;
    private readonly Mock<IDateTime> _dateTimeMock;

    public AuditRetentionJobTests()
    {
        _dbContextMock = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
        _fileStorageMock = new Mock<IFileStorage>();
        _loggerMock = new Mock<ILogger<AuditRetentionJob>>();
        _dateTimeMock = new Mock<IDateTime>();
        _dateTimeMock.Setup(d => d.UtcNow).Returns(DateTimeOffset.UtcNow);
    }

    #region ValidateConfiguration Tests

    [Fact]
    public void ValidateConfiguration_NegativeArchiveAfterDays_ShouldThrow()
    {
        // Arrange
        var settings = new AuditRetentionSettings
        {
            Enabled = true,
            ArchiveAfterDays = -1,
            DeleteAfterDays = 365,
            BatchSize = 1000
        };

        var job = CreateJob(settings);

        // Act
        var act = () => InvokeValidateConfiguration(job);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ArchiveAfterDays*cannot be negative*");
    }

    [Fact]
    public void ValidateConfiguration_NegativeDeleteAfterDays_ShouldThrow()
    {
        // Arrange
        var settings = new AuditRetentionSettings
        {
            Enabled = true,
            ArchiveAfterDays = 90,
            DeleteAfterDays = -1,
            BatchSize = 1000
        };

        var job = CreateJob(settings);

        // Act
        var act = () => InvokeValidateConfiguration(job);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DeleteAfterDays*cannot be negative*");
    }

    [Fact]
    public void ValidateConfiguration_DeleteBeforeArchive_ShouldThrow()
    {
        // Arrange
        var settings = new AuditRetentionSettings
        {
            Enabled = true,
            ArchiveAfterDays = 365,
            DeleteAfterDays = 90, // Less than ArchiveAfterDays
            BatchSize = 1000
        };

        var job = CreateJob(settings);

        // Act
        var act = () => InvokeValidateConfiguration(job);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DeleteAfterDays*must be greater than ArchiveAfterDays*");
    }

    [Fact]
    public void ValidateConfiguration_DeleteEqualsArchive_ShouldThrow()
    {
        // Arrange
        var settings = new AuditRetentionSettings
        {
            Enabled = true,
            ArchiveAfterDays = 90,
            DeleteAfterDays = 90, // Equal to ArchiveAfterDays
            BatchSize = 1000
        };

        var job = CreateJob(settings);

        // Act
        var act = () => InvokeValidateConfiguration(job);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DeleteAfterDays*must be greater than ArchiveAfterDays*");
    }

    [Fact]
    public void ValidateConfiguration_ZeroBatchSize_ShouldThrow()
    {
        // Arrange
        var settings = new AuditRetentionSettings
        {
            Enabled = true,
            ArchiveAfterDays = 90,
            DeleteAfterDays = 365,
            BatchSize = 0
        };

        var job = CreateJob(settings);

        // Act
        var act = () => InvokeValidateConfiguration(job);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BatchSize*must be greater than 0*");
    }

    [Fact]
    public void ValidateConfiguration_NegativeBatchSize_ShouldThrow()
    {
        // Arrange
        var settings = new AuditRetentionSettings
        {
            Enabled = true,
            ArchiveAfterDays = 90,
            DeleteAfterDays = 365,
            BatchSize = -100
        };

        var job = CreateJob(settings);

        // Act
        var act = () => InvokeValidateConfiguration(job);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BatchSize*must be greater than 0*");
    }

    [Fact]
    public void ValidateConfiguration_ValidSettings_ShouldNotThrow()
    {
        // Arrange
        var settings = new AuditRetentionSettings
        {
            Enabled = true,
            ArchiveAfterDays = 90,
            DeleteAfterDays = 365,
            BatchSize = 1000
        };

        var job = CreateJob(settings);

        // Act
        var act = () => InvokeValidateConfiguration(job);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_LargeBatchSize_ShouldLogWarning()
    {
        // Arrange
        var settings = new AuditRetentionSettings
        {
            Enabled = true,
            ArchiveAfterDays = 90,
            DeleteAfterDays = 365,
            BatchSize = 15000 // > 10000
        };

        var job = CreateJob(settings);

        // Act
        InvokeValidateConfiguration(job);

        // Assert - Should log warning for large batch size
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("BatchSize")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateConfiguration_ShortArchivePeriod_ShouldLogWarning()
    {
        // Arrange
        var settings = new AuditRetentionSettings
        {
            Enabled = true,
            ArchiveAfterDays = 15, // < 30
            DeleteAfterDays = 365,
            BatchSize = 1000
        };

        var job = CreateJob(settings);

        // Act
        InvokeValidateConfiguration(job);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ArchiveAfterDays")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateConfiguration_ShortDeletePeriod_ShouldLogWarning()
    {
        // Arrange
        var settings = new AuditRetentionSettings
        {
            Enabled = true,
            ArchiveAfterDays = 30,
            DeleteAfterDays = 60, // < 90
            BatchSize = 1000
        };

        var job = CreateJob(settings);

        // Act
        InvokeValidateConfiguration(job);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DeleteAfterDays")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region IsTransientError Tests

    [Theory]
    [InlineData("Transaction was deadlocked")]
    [InlineData("DEADLOCK detected")]
    public void IsTransientError_DeadlockMessage_ShouldReturnTrue(string message)
    {
        // Arrange
        var exception = new Exception(message);

        // Act
        var result = InvokeIsTransientError(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Execution Timeout Expired")]
    [InlineData("timeout expired")]
    public void IsTransientError_TimeoutMessage_ShouldReturnTrue(string message)
    {
        // Arrange
        var exception = new Exception(message);

        // Act
        var result = InvokeIsTransientError(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("lock request time out period exceeded")]
    [InlineData("Lock request time out")]
    public void IsTransientError_LockTimeoutMessage_ShouldReturnTrue(string message)
    {
        // Arrange
        var exception = new Exception(message);

        // Act
        var result = InvokeIsTransientError(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Invalid operation")]
    [InlineData("Object reference not set")]
    [InlineData("The input string was not in a correct format")]
    public void IsTransientError_NonTransientMessage_ShouldReturnFalse(string message)
    {
        // Arrange
        var exception = new Exception(message);

        // Act
        var result = InvokeIsTransientError(exception);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldReturnEarly()
    {
        // Arrange
        var settings = new AuditRetentionSettings { Enabled = false };
        var job = CreateJob(settings);

        // Act
        await job.ExecuteAsync();

        // Assert - Should log that job is disabled
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("disabled")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region AuditRetentionSettings Tests

    [Fact]
    public void AuditRetentionSettings_DefaultValues_ShouldBeReasonable()
    {
        // Act
        var settings = new AuditRetentionSettings();

        // Assert - Check documented defaults match actual defaults
        settings.ArchiveAfterDays.Should().Be(90);
        settings.DeleteAfterDays.Should().Be(365);
        settings.BatchSize.Should().Be(10000);
        settings.Enabled.Should().BeTrue();
        settings.EnableArchiving.Should().BeTrue();
        settings.ExportBeforeDelete.Should().BeFalse();
        settings.ExportPath.Should().Be("audit-archives");
    }

    [Fact]
    public void AuditRetentionSettings_SectionName_ShouldBeCorrect()
    {
        // Assert
        AuditRetentionSettings.SectionName.Should().Be("AuditRetention");
    }

    #endregion

    #region Helper Methods

    private AuditRetentionJob CreateJob(AuditRetentionSettings settings)
    {
        var options = Microsoft.Extensions.Options.Options.Create(settings);

        return new AuditRetentionJob(
            _dbContextMock.Object,
            _fileStorageMock.Object,
            options,
            _loggerMock.Object,
            _dateTimeMock.Object);
    }

    private static void InvokeValidateConfiguration(AuditRetentionJob job)
    {
        var method = typeof(AuditRetentionJob)
            .GetMethod("ValidateConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);
        try
        {
            method?.Invoke(job, null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static bool InvokeIsTransientError(Exception exception)
    {
        var method = typeof(AuditRetentionJob)
            .GetMethod("IsTransientError", BindingFlags.NonPublic | BindingFlags.Static);
        return (bool)method?.Invoke(null, [exception])!;
    }

    #endregion
}
