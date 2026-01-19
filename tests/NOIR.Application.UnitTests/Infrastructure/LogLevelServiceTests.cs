namespace NOIR.Application.UnitTests.Infrastructure;

using NOIR.Application.Features.DeveloperLogs.DTOs;
using NOIR.Infrastructure.Logging;
using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Unit tests for LogLevelService.
/// Tests dynamic log level management for Serilog.
/// </summary>
public class LogLevelServiceTests
{
    private readonly LoggingLevelSwitch _globalLevelSwitch;
    private readonly DeveloperLogSettings _settings;
    private readonly Mock<ILogger<LogLevelService>> _loggerMock;

    public LogLevelServiceTests()
    {
        _globalLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        _settings = new DeveloperLogSettings
        {
            DefaultMinimumLevel = "Information",
            LevelOverrides = new Dictionary<string, string>
            {
                ["Microsoft"] = "Warning",
                ["System"] = "Error"
            }
        };
        _loggerMock = new Mock<ILogger<LogLevelService>>();
    }

    private LogLevelService CreateSut()
    {
        return new LogLevelService(
            _globalLevelSwitch,
            Options.Create(_settings),
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldImplementILogLevelService()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.Should().BeAssignableTo<ILogLevelService>();
    }

    [Fact]
    public void Constructor_ShouldImplementISingletonService()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.Should().BeAssignableTo<ISingletonService>();
    }

    [Fact]
    public void Constructor_ShouldSetGlobalLevelFromSettings()
    {
        // Arrange
        var levelSwitch = new LoggingLevelSwitch();
        var settings = new DeveloperLogSettings { DefaultMinimumLevel = "Warning" };

        // Act
        var sut = new LogLevelService(levelSwitch, Options.Create(settings), _loggerMock.Object);

        // Assert
        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Warning);
    }

    [Fact]
    public void Constructor_ShouldInitializeSourceOverrides()
    {
        // Act
        var sut = CreateSut();

        // Assert
        var overrides = sut.GetOverrides().ToList();
        overrides.Should().HaveCount(2);
        overrides.Should().Contain(o => o.SourcePrefix == "Microsoft" && o.Level == "Warning");
        overrides.Should().Contain(o => o.SourcePrefix == "System" && o.Level == "Error");
    }

    [Fact]
    public void Constructor_WithInvalidLevelInSettings_ShouldDefaultToInformation()
    {
        // Arrange
        var levelSwitch = new LoggingLevelSwitch();
        var settings = new DeveloperLogSettings { DefaultMinimumLevel = "InvalidLevel" };

        // Act
        var sut = new LogLevelService(levelSwitch, Options.Create(settings), _loggerMock.Object);

        // Assert
        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Information);
    }

    #endregion

    #region GetCurrentLevel Tests

    [Fact]
    public void GetCurrentLevel_ShouldReturnCurrentGlobalLevel()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var level = sut.GetCurrentLevel();

        // Assert
        level.Should().Be("Information");
    }

    [Fact]
    public void GetCurrentLevel_AfterChange_ShouldReturnNewLevel()
    {
        // Arrange
        var sut = CreateSut();
        sut.SetLevel("Error");

        // Act
        var level = sut.GetCurrentLevel();

        // Assert
        level.Should().Be("Error");
    }

    #endregion

    #region SetLevel Tests

    [Fact]
    public void SetLevel_WithValidLevel_ShouldReturnTrue()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.SetLevel("Warning");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SetLevel_WithValidLevel_ShouldUpdateGlobalSwitch()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.SetLevel("Error");

        // Assert
        _globalLevelSwitch.MinimumLevel.Should().Be(LogEventLevel.Error);
    }

    [Fact]
    public void SetLevel_WithInvalidLevel_ShouldReturnFalse()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.SetLevel("InvalidLevel");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetLevel_WithInvalidLevel_ShouldNotChangeCurrentLevel()
    {
        // Arrange
        var sut = CreateSut();
        var originalLevel = _globalLevelSwitch.MinimumLevel;

        // Act
        sut.SetLevel("InvalidLevel");

        // Assert
        _globalLevelSwitch.MinimumLevel.Should().Be(originalLevel);
    }

    [Fact]
    public void SetLevel_ShouldRaiseOnLevelChangedEvent()
    {
        // Arrange
        var sut = CreateSut();
        string? receivedLevel = null;
        sut.OnLevelChanged += level => receivedLevel = level;

        // Act
        sut.SetLevel("Debug");

        // Assert
        receivedLevel.Should().Be("Debug");
    }

    [Fact]
    public void SetLevel_WithInvalidLevel_ShouldNotRaiseEvent()
    {
        // Arrange
        var sut = CreateSut();
        var eventRaised = false;
        sut.OnLevelChanged += _ => eventRaised = true;

        // Act
        sut.SetLevel("InvalidLevel");

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Theory]
    [InlineData("Verbose", LogEventLevel.Verbose)]
    [InlineData("Debug", LogEventLevel.Debug)]
    [InlineData("Information", LogEventLevel.Information)]
    [InlineData("Warning", LogEventLevel.Warning)]
    [InlineData("Error", LogEventLevel.Error)]
    [InlineData("Fatal", LogEventLevel.Fatal)]
    public void SetLevel_WithStandardLevel_ShouldSetCorrectLevel(string levelName, LogEventLevel expected)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.SetLevel(levelName);

        // Assert
        result.Should().BeTrue();
        _globalLevelSwitch.MinimumLevel.Should().Be(expected);
    }

    [Theory]
    [InlineData("verbose")]
    [InlineData("VERBOSE")]
    [InlineData("Verbose")]
    public void SetLevel_ShouldBeCaseInsensitive(string levelName)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.SetLevel(levelName);

        // Assert
        result.Should().BeTrue();
        _globalLevelSwitch.MinimumLevel.Should().Be(LogEventLevel.Verbose);
    }

    [Theory]
    [InlineData("TRACE", LogEventLevel.Verbose)]
    [InlineData("DBG", LogEventLevel.Debug)]
    [InlineData("INF", LogEventLevel.Information)]
    [InlineData("INFO", LogEventLevel.Information)]
    [InlineData("WRN", LogEventLevel.Warning)]
    [InlineData("WARN", LogEventLevel.Warning)]
    [InlineData("ERR", LogEventLevel.Error)]
    [InlineData("FTL", LogEventLevel.Fatal)]
    [InlineData("CRIT", LogEventLevel.Fatal)]
    [InlineData("CRITICAL", LogEventLevel.Fatal)]
    public void SetLevel_ShouldSupportAliases(string alias, LogEventLevel expected)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.SetLevel(alias);

        // Assert
        result.Should().BeTrue();
        _globalLevelSwitch.MinimumLevel.Should().Be(expected);
    }

    [Fact]
    public void SetLevel_ShouldLogLevelChange()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.SetLevel("Warning");

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("log level changed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetLevel_WithInvalidLevel_ShouldLogWarning()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.SetLevel("BadLevel");

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid log level")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetAvailableLevels Tests

    [Fact]
    public void GetAvailableLevels_ShouldReturnAllLogEventLevels()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var levels = sut.GetAvailableLevels();

        // Assert
        levels.Should().Contain("Verbose");
        levels.Should().Contain("Debug");
        levels.Should().Contain("Information");
        levels.Should().Contain("Warning");
        levels.Should().Contain("Error");
        levels.Should().Contain("Fatal");
    }

    [Fact]
    public void GetAvailableLevels_ShouldReturnCorrectCount()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var levels = sut.GetAvailableLevels();

        // Assert
        levels.Should().HaveCount(6);
    }

    #endregion

    #region GetOverrides Tests

    [Fact]
    public void GetOverrides_ShouldReturnConfiguredOverrides()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var overrides = sut.GetOverrides().ToList();

        // Assert
        overrides.Should().HaveCount(2);
    }

    [Fact]
    public void GetOverrides_ShouldReturnOrderedBySourcePrefix()
    {
        // Arrange
        var settings = new DeveloperLogSettings
        {
            DefaultMinimumLevel = "Information",
            LevelOverrides = new Dictionary<string, string>
            {
                ["Zebra"] = "Warning",
                ["Apple"] = "Error",
                ["Microsoft"] = "Debug"
            }
        };
        var sut = new LogLevelService(_globalLevelSwitch, Options.Create(settings), _loggerMock.Object);

        // Act
        var overrides = sut.GetOverrides().ToList();

        // Assert
        overrides[0].SourcePrefix.Should().Be("Apple");
        overrides[1].SourcePrefix.Should().Be("Microsoft");
        overrides[2].SourcePrefix.Should().Be("Zebra");
    }

    #endregion

    #region SetOverride Tests

    [Fact]
    public void SetOverride_WithValidInput_ShouldReturnTrue()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.SetOverride("NOIR.Tests", "Debug");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SetOverride_ShouldAddNewOverride()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.SetOverride("NOIR.Tests", "Debug");

        // Assert
        var overrides = sut.GetOverrides().ToList();
        overrides.Should().Contain(o => o.SourcePrefix == "NOIR.Tests" && o.Level == "Debug");
    }

    [Fact]
    public void SetOverride_WithExistingSource_ShouldUpdateLevel()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.SetOverride("Microsoft", "Error");

        // Assert
        var overrides = sut.GetOverrides().ToList();
        var microsoftOverride = overrides.First(o => o.SourcePrefix == "Microsoft");
        microsoftOverride.Level.Should().Be("Error");
    }

    [Fact]
    public void SetOverride_WithEmptySourcePrefix_ShouldReturnFalse()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.SetOverride("", "Debug");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetOverride_WithWhitespaceSourcePrefix_ShouldReturnFalse()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.SetOverride("   ", "Debug");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetOverride_WithInvalidLevel_ShouldReturnFalse()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.SetOverride("NOIR.Tests", "InvalidLevel");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetOverride_WithInvalidLevel_ShouldNotAddOverride()
    {
        // Arrange
        var sut = CreateSut();
        var originalCount = sut.GetOverrides().Count();

        // Act
        sut.SetOverride("NOIR.Tests", "InvalidLevel");

        // Assert
        sut.GetOverrides().Count().Should().Be(originalCount);
    }

    [Fact]
    public void SetOverride_ShouldLogOverrideSet()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.SetOverride("NOIR.Tests", "Debug");

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("override set")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region RemoveOverride Tests

    [Fact]
    public void RemoveOverride_WhenExists_ShouldReturnTrue()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.RemoveOverride("Microsoft");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RemoveOverride_WhenExists_ShouldRemoveOverride()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.RemoveOverride("Microsoft");

        // Assert
        var overrides = sut.GetOverrides().ToList();
        overrides.Should().NotContain(o => o.SourcePrefix == "Microsoft");
    }

    [Fact]
    public void RemoveOverride_WhenNotExists_ShouldReturnFalse()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.RemoveOverride("NonExistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveOverride_ShouldLogRemoval()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.RemoveOverride("Microsoft");

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("override removed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetGlobalSwitch Tests

    [Fact]
    public void GetGlobalSwitch_ShouldReturnTheSwitch()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.GetGlobalSwitch();

        // Assert
        result.Should().BeSameAs(_globalLevelSwitch);
    }

    #endregion

    #region GetSourceSwitch Tests

    [Fact]
    public void GetSourceSwitch_WhenExists_ShouldReturnSwitch()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.GetSourceSwitch("Microsoft");

        // Assert
        result.Should().NotBeNull();
        result!.MinimumLevel.Should().Be(LogEventLevel.Warning);
    }

    [Fact]
    public void GetSourceSwitch_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.GetSourceSwitch("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllSourceSwitches Tests

    [Fact]
    public void GetAllSourceSwitches_ShouldReturnAllSwitches()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var switches = sut.GetAllSourceSwitches();

        // Assert
        switches.Should().HaveCount(2);
        switches.Should().ContainKey("Microsoft");
        switches.Should().ContainKey("System");
    }

    [Fact]
    public void GetAllSourceSwitches_ShouldReturnReadOnlyDictionary()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var switches = sut.GetAllSourceSwitches();

        // Assert
        switches.Should().BeAssignableTo<IReadOnlyDictionary<string, LoggingLevelSwitch>>();
    }

    #endregion

    #region OnLevelChanged Event Tests

    [Fact]
    public void OnLevelChanged_ShouldSupportMultipleSubscribers()
    {
        // Arrange
        var sut = CreateSut();
        var callCount = 0;
        sut.OnLevelChanged += _ => callCount++;
        sut.OnLevelChanged += _ => callCount++;

        // Act
        sut.SetLevel("Debug");

        // Assert
        callCount.Should().Be(2);
    }

    [Fact]
    public void OnLevelChanged_ShouldPassCorrectLevel()
    {
        // Arrange
        var sut = CreateSut();
        var receivedLevels = new List<string>();
        sut.OnLevelChanged += level => receivedLevels.Add(level);

        // Act
        sut.SetLevel("Debug");
        sut.SetLevel("Warning");
        sut.SetLevel("Error");

        // Assert
        receivedLevels.Should().Equal("Debug", "Warning", "Error");
    }

    #endregion
}
