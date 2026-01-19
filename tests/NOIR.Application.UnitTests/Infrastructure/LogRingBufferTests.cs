namespace NOIR.Application.UnitTests.Infrastructure;

using NOIR.Application.Features.DeveloperLogs.DTOs;
using NOIR.Infrastructure.Logging;

/// <summary>
/// Unit tests for LogRingBuffer.
/// Tests the thread-safe circular buffer for log entries.
/// </summary>
public class LogRingBufferTests
{
    private readonly DeveloperLogSettings _settings;

    public LogRingBufferTests()
    {
        _settings = new DeveloperLogSettings
        {
            BufferCapacity = 100
        };
    }

    private LogRingBuffer CreateSut()
    {
        return new LogRingBuffer(Options.Create(_settings));
    }

    private static LogEntryDto CreateLogEntry(
        DevLogLevel level = DevLogLevel.Information,
        string message = "Test message",
        ExceptionDto? exception = null)
    {
        return new LogEntryDto
        {
            Id = 0, // Will be assigned by buffer
            Timestamp = DateTimeOffset.UtcNow,
            Level = level,
            Message = message,
            SourceContext = "NOIR.Tests.TestClass",
            Exception = exception
        };
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
    public void Constructor_ShouldImplementILogRingBuffer()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.Should().BeAssignableTo<ILogRingBuffer>();
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
    public void Constructor_ShouldInitializeEmptyBuffer()
    {
        // Act
        var sut = CreateSut();
        var stats = sut.GetStats();

        // Assert
        stats.TotalEntries.Should().Be(0);
        stats.MaxCapacity.Should().Be(100);
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_ShouldAddEntryToBuffer()
    {
        // Arrange
        var sut = CreateSut();
        var entry = CreateLogEntry();

        // Act
        sut.Add(entry);

        // Assert
        var stats = sut.GetStats();
        stats.TotalEntries.Should().Be(1);
    }

    [Fact]
    public void Add_ShouldAssignIncrementingIds()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Add(CreateLogEntry());
        sut.Add(CreateLogEntry());
        sut.Add(CreateLogEntry());

        // Assert
        var entries = sut.GetRecentEntries(3).ToList();
        entries.Should().HaveCount(3);
        entries[0].Id.Should().Be(3);
        entries[1].Id.Should().Be(2);
        entries[2].Id.Should().Be(1);
    }

    [Fact]
    public void Add_ShouldUpdateLevelCounts()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Add(CreateLogEntry(DevLogLevel.Information));
        sut.Add(CreateLogEntry(DevLogLevel.Warning));
        sut.Add(CreateLogEntry(DevLogLevel.Error));
        sut.Add(CreateLogEntry(DevLogLevel.Error));

        // Assert
        var stats = sut.GetStats();
        stats.EntriesByLevel["Information"].Should().Be(1);
        stats.EntriesByLevel["Warning"].Should().Be(1);
        stats.EntriesByLevel["Error"].Should().Be(2);
    }

    [Fact]
    public void Add_ShouldRaiseOnEntryAddedEvent()
    {
        // Arrange
        var sut = CreateSut();
        LogEntryDto? receivedEntry = null;
        sut.OnEntryAdded += entry => receivedEntry = entry;

        var newEntry = CreateLogEntry(message: "Event test");

        // Act
        sut.Add(newEntry);

        // Assert
        receivedEntry.Should().NotBeNull();
        receivedEntry!.Message.Should().Be("Event test");
    }

    [Fact]
    public void Add_ShouldTrackMemoryUsage()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Add(CreateLogEntry(message: "Short"));
        var statsAfterFirst = sut.GetStats();

        sut.Add(CreateLogEntry(message: "This is a much longer message that takes more memory"));
        var statsAfterSecond = sut.GetStats();

        // Assert
        statsAfterSecond.MemoryUsageBytes.Should().BeGreaterThan(statsAfterFirst.MemoryUsageBytes);
    }

    #endregion

    #region Buffer Overflow Tests

    [Fact]
    public void Add_WhenBufferFull_ShouldOverwriteOldestEntry()
    {
        // Arrange
        var smallSettings = new DeveloperLogSettings { BufferCapacity = 3 };
        var sut = new LogRingBuffer(Options.Create(smallSettings));

        // Act
        sut.Add(CreateLogEntry(message: "Entry 1"));
        sut.Add(CreateLogEntry(message: "Entry 2"));
        sut.Add(CreateLogEntry(message: "Entry 3"));
        sut.Add(CreateLogEntry(message: "Entry 4")); // Should overwrite Entry 1

        // Assert
        var stats = sut.GetStats();
        stats.TotalEntries.Should().Be(3);

        var entries = sut.GetRecentEntries(10).ToList();
        entries.Should().HaveCount(3);
        entries.Select(e => e.Message).Should().NotContain("Entry 1");
        entries.Select(e => e.Message).Should().Contain("Entry 4");
    }

    [Fact]
    public void Add_WhenBufferFull_ShouldDecrementOldLevelCount()
    {
        // Arrange
        var smallSettings = new DeveloperLogSettings { BufferCapacity = 2 };
        var sut = new LogRingBuffer(Options.Create(smallSettings));

        // Act
        sut.Add(CreateLogEntry(DevLogLevel.Error, "Error 1"));
        sut.Add(CreateLogEntry(DevLogLevel.Warning, "Warning 1"));

        var statsBefore = sut.GetStats();
        statsBefore.EntriesByLevel["Error"].Should().Be(1);

        // Overwrite the error entry
        sut.Add(CreateLogEntry(DevLogLevel.Information, "Info 1"));

        // Assert
        var statsAfter = sut.GetStats();
        statsAfter.EntriesByLevel.Should().NotContainKey("Error");
        statsAfter.EntriesByLevel["Information"].Should().Be(1);
        statsAfter.EntriesByLevel["Warning"].Should().Be(1);
    }

    [Fact]
    public void Add_WhenBufferOverflows_ShouldMaintainCorrectOrder()
    {
        // Arrange
        var smallSettings = new DeveloperLogSettings { BufferCapacity = 3 };
        var sut = new LogRingBuffer(Options.Create(smallSettings));

        // Act - Add more entries than capacity
        for (int i = 1; i <= 5; i++)
        {
            sut.Add(CreateLogEntry(message: $"Entry {i}"));
        }

        // Assert - Should have entries 3, 4, 5 (newest first when retrieved)
        var entries = sut.GetRecentEntries(10).ToList();
        entries.Should().HaveCount(3);
        entries[0].Message.Should().Be("Entry 5");
        entries[1].Message.Should().Be("Entry 4");
        entries[2].Message.Should().Be("Entry 3");
    }

    #endregion

    #region GetRecentEntries Tests

    [Fact]
    public void GetRecentEntries_WhenBufferEmpty_ShouldReturnEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var entries = sut.GetRecentEntries(10);

        // Assert
        entries.Should().BeEmpty();
    }

    [Fact]
    public void GetRecentEntries_ShouldReturnNewestFirst()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(message: "First"));
        sut.Add(CreateLogEntry(message: "Second"));
        sut.Add(CreateLogEntry(message: "Third"));

        // Act
        var entries = sut.GetRecentEntries(10).ToList();

        // Assert
        entries[0].Message.Should().Be("Third");
        entries[1].Message.Should().Be("Second");
        entries[2].Message.Should().Be("First");
    }

    [Fact]
    public void GetRecentEntries_ShouldLimitResults()
    {
        // Arrange
        var sut = CreateSut();
        for (int i = 0; i < 10; i++)
        {
            sut.Add(CreateLogEntry(message: $"Entry {i}"));
        }

        // Act
        var entries = sut.GetRecentEntries(3);

        // Assert
        entries.Should().HaveCount(3);
    }

    [Fact]
    public void GetRecentEntries_WhenCountExceedsEntries_ShouldReturnAllEntries()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(message: "Entry 1"));
        sut.Add(CreateLogEntry(message: "Entry 2"));

        // Act
        var entries = sut.GetRecentEntries(100);

        // Assert
        entries.Should().HaveCount(2);
    }

    #endregion

    #region GetEntriesBefore Tests

    [Fact]
    public void GetEntriesBefore_WhenBufferEmpty_ShouldReturnEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var entries = sut.GetEntriesBefore(100, 10);

        // Assert
        entries.Should().BeEmpty();
    }

    [Fact]
    public void GetEntriesBefore_ShouldReturnEntriesWithLowerId()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(message: "Entry 1")); // ID 1
        sut.Add(CreateLogEntry(message: "Entry 2")); // ID 2
        sut.Add(CreateLogEntry(message: "Entry 3")); // ID 3
        sut.Add(CreateLogEntry(message: "Entry 4")); // ID 4

        // Act
        var entries = sut.GetEntriesBefore(3, 10).ToList();

        // Assert
        entries.Should().HaveCount(2);
        entries.All(e => e.Id < 3).Should().BeTrue();
    }

    [Fact]
    public void GetEntriesBefore_ShouldLimitResults()
    {
        // Arrange
        var sut = CreateSut();
        for (int i = 0; i < 10; i++)
        {
            sut.Add(CreateLogEntry(message: $"Entry {i}"));
        }

        // Act
        var entries = sut.GetEntriesBefore(10, 3);

        // Assert
        entries.Should().HaveCount(3);
    }

    #endregion

    #region GetFiltered Tests

    [Fact]
    public void GetFiltered_WithNoFilters_ShouldReturnAllEntries()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(DevLogLevel.Information, "Info"));
        sut.Add(CreateLogEntry(DevLogLevel.Warning, "Warning"));
        sut.Add(CreateLogEntry(DevLogLevel.Error, "Error"));

        // Act
        var entries = sut.GetFiltered();

        // Assert
        entries.Should().HaveCount(3);
    }

    [Fact]
    public void GetFiltered_WithMinLevel_ShouldFilterByLevel()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(DevLogLevel.Debug, "Debug"));
        sut.Add(CreateLogEntry(DevLogLevel.Information, "Info"));
        sut.Add(CreateLogEntry(DevLogLevel.Warning, "Warning"));
        sut.Add(CreateLogEntry(DevLogLevel.Error, "Error"));

        // Act
        var entries = sut.GetFiltered(minLevel: DevLogLevel.Warning);

        // Assert
        entries.Should().HaveCount(2);
        entries.All(e => e.Level >= DevLogLevel.Warning).Should().BeTrue();
    }

    [Fact]
    public void GetFiltered_WithSources_ShouldFilterBySourcePrefix()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(new LogEntryDto
        {
            Id = 0,
            Timestamp = DateTimeOffset.UtcNow,
            Level = DevLogLevel.Information,
            Message = "Test 1",
            SourceContext = "NOIR.Application.Services.UserService"
        });
        sut.Add(new LogEntryDto
        {
            Id = 0,
            Timestamp = DateTimeOffset.UtcNow,
            Level = DevLogLevel.Information,
            Message = "Test 2",
            SourceContext = "NOIR.Infrastructure.Persistence"
        });
        sut.Add(new LogEntryDto
        {
            Id = 0,
            Timestamp = DateTimeOffset.UtcNow,
            Level = DevLogLevel.Information,
            Message = "Test 3",
            SourceContext = "Microsoft.EntityFrameworkCore"
        });

        // Act
        var entries = sut.GetFiltered(sources: new[] { "NOIR.Application" });

        // Assert
        entries.Should().HaveCount(1);
        entries.First().SourceContext.Should().StartWith("NOIR.Application");
    }

    [Fact]
    public void GetFiltered_WithSearchPattern_ShouldFilterByMessage()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(message: "User logged in successfully"));
        sut.Add(CreateLogEntry(message: "User logged out"));
        sut.Add(CreateLogEntry(message: "Database connection failed"));

        // Act
        var entries = sut.GetFiltered(searchPattern: "logged");

        // Assert
        entries.Should().HaveCount(2);
    }

    [Fact]
    public void GetFiltered_WithRegexSearchPattern_ShouldSupportRegex()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(message: "User 123 logged in"));
        sut.Add(CreateLogEntry(message: "User 456 logged out"));
        sut.Add(CreateLogEntry(message: "Database error"));

        // Act
        var entries = sut.GetFiltered(searchPattern: @"User \d+ logged");

        // Assert
        entries.Should().HaveCount(2);
    }

    [Fact]
    public void GetFiltered_WithInvalidRegex_ShouldTreatAsLiteralString()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(message: "Test [invalid regex"));

        // Act - Invalid regex should not throw
        var entries = sut.GetFiltered(searchPattern: "[invalid");

        // Assert
        entries.Should().HaveCount(1);
    }

    [Fact]
    public void GetFiltered_WithExceptionsOnly_ShouldFilterByException()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(message: "Normal log"));
        sut.Add(CreateLogEntry(
            message: "Error occurred",
            exception: new ExceptionDto { Type = "System.Exception", Message = "Test error" }));

        // Act
        var entries = sut.GetFiltered(exceptionsOnly: true);

        // Assert
        entries.Should().HaveCount(1);
        entries.First().Exception.Should().NotBeNull();
    }

    [Fact]
    public void GetFiltered_WithMaxCount_ShouldLimitResults()
    {
        // Arrange
        var sut = CreateSut();
        for (int i = 0; i < 20; i++)
        {
            sut.Add(CreateLogEntry(message: $"Entry {i}"));
        }

        // Act
        var entries = sut.GetFiltered(maxCount: 5);

        // Assert
        entries.Should().HaveCount(5);
    }

    [Fact]
    public void GetFiltered_ShouldSearchExceptionMessages()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(
            message: "An error occurred",
            exception: new ExceptionDto { Type = "ArgumentException", Message = "Value cannot be null" }));
        sut.Add(CreateLogEntry(message: "Normal message"));

        // Act
        var entries = sut.GetFiltered(searchPattern: "cannot be null");

        // Assert
        entries.Should().HaveCount(1);
    }

    #endregion

    #region GetErrorClusters Tests

    [Fact]
    public void GetErrorClusters_WhenNoErrors_ShouldReturnEmpty()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(DevLogLevel.Information, "Info message"));

        // Act
        var clusters = sut.GetErrorClusters();

        // Assert
        clusters.Should().BeEmpty();
    }

    [Fact]
    public void GetErrorClusters_ShouldClusterSimilarErrors()
    {
        // Arrange
        var sut = CreateSut();

        // Add similar errors with different GUIDs
        sut.Add(CreateLogEntry(DevLogLevel.Error, "User 123e4567-e89b-12d3-a456-426614174000 not found"));
        sut.Add(CreateLogEntry(DevLogLevel.Error, "User 987e6543-e21a-34b5-c678-901234567890 not found"));

        // Act
        var clusters = sut.GetErrorClusters().ToList();

        // Assert
        clusters.Should().HaveCount(1);
        clusters[0].Count.Should().Be(2);
    }

    [Fact]
    public void GetErrorClusters_ShouldLimitResults()
    {
        // Arrange
        var sut = CreateSut();

        // Add many different error patterns
        for (int i = 0; i < 20; i++)
        {
            sut.Add(CreateLogEntry(DevLogLevel.Error, $"Unique error pattern {i}"));
        }

        // Act
        var clusters = sut.GetErrorClusters(maxClusters: 5);

        // Assert - Clustering may group similar patterns, so result count may be <= limit
        clusters.Should().HaveCountLessThanOrEqualTo(5);
        clusters.Should().NotBeEmpty();
    }

    [Fact]
    public void GetErrorClusters_ShouldOrderByCountDescending()
    {
        // Arrange
        var sut = CreateSut();

        sut.Add(CreateLogEntry(DevLogLevel.Error, "Rare error"));
        for (int i = 0; i < 5; i++)
        {
            sut.Add(CreateLogEntry(DevLogLevel.Error, "Common error"));
        }

        // Act
        var clusters = sut.GetErrorClusters().ToList();

        // Assert
        clusters[0].Pattern.Should().Contain("Common");
        clusters[0].Count.Should().BeGreaterThan(clusters[1].Count);
    }

    [Fact]
    public void GetErrorClusters_ShouldDetermineSeverity()
    {
        // Arrange
        var sut = CreateSut();

        // Add critical level of errors (>100)
        for (int i = 0; i < 5; i++)
        {
            sut.Add(CreateLogEntry(DevLogLevel.Error, "Low severity error"));
        }

        // Act
        var clusters = sut.GetErrorClusters().ToList();

        // Assert
        clusters[0].Severity.Should().Be("low"); // count <= 5
    }

    [Fact]
    public void GetErrorClusters_ShouldTrackFirstAndLastSeen()
    {
        // Arrange
        var sut = CreateSut();

        sut.Add(CreateLogEntry(DevLogLevel.Error, "Repeated error"));
        Thread.Sleep(10); // Small delay to ensure different timestamps
        sut.Add(CreateLogEntry(DevLogLevel.Error, "Repeated error"));

        // Act
        var clusters = sut.GetErrorClusters().ToList();

        // Assert
        clusters[0].FirstSeen.Should().BeBefore(clusters[0].LastSeen);
    }

    [Fact]
    public void GetErrorClusters_ShouldLimitSamples()
    {
        // Arrange
        var sut = CreateSut();

        for (int i = 0; i < 10; i++)
        {
            sut.Add(CreateLogEntry(DevLogLevel.Error, "Same error"));
        }

        // Act
        var clusters = sut.GetErrorClusters().ToList();

        // Assert
        clusters[0].Samples.Should().HaveCountLessThanOrEqualTo(5);
    }

    #endregion

    #region GetStats Tests

    [Fact]
    public void GetStats_WhenEmpty_ShouldReturnZeroStats()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var stats = sut.GetStats();

        // Assert
        stats.TotalEntries.Should().Be(0);
        stats.MaxCapacity.Should().Be(100);
        stats.MemoryUsageBytes.Should().Be(0);
        stats.OldestEntry.Should().BeNull();
        stats.NewestEntry.Should().BeNull();
    }

    [Fact]
    public void GetStats_ShouldReturnCorrectEntryCount()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry());
        sut.Add(CreateLogEntry());
        sut.Add(CreateLogEntry());

        // Act
        var stats = sut.GetStats();

        // Assert
        stats.TotalEntries.Should().Be(3);
    }

    [Fact]
    public void GetStats_ShouldReturnOldestAndNewestTimestamps()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry());
        sut.Add(CreateLogEntry());

        // Act
        var stats = sut.GetStats();

        // Assert
        stats.OldestEntry.Should().NotBeNull();
        stats.NewestEntry.Should().NotBeNull();
        stats.OldestEntry.Should().BeOnOrBefore(stats.NewestEntry!.Value);
    }

    [Fact]
    public void GetStats_ShouldOnlyIncludeNonZeroLevelCounts()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(DevLogLevel.Information));
        sut.Add(CreateLogEntry(DevLogLevel.Error));

        // Act
        var stats = sut.GetStats();

        // Assert
        stats.EntriesByLevel.Should().ContainKey("Information");
        stats.EntriesByLevel.Should().ContainKey("Error");
        stats.EntriesByLevel.Should().NotContainKey("Debug");
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry());
        sut.Add(CreateLogEntry());
        sut.Add(CreateLogEntry());

        // Act
        sut.Clear();

        // Assert
        var stats = sut.GetStats();
        stats.TotalEntries.Should().Be(0);
    }

    [Fact]
    public void Clear_ShouldResetLevelCounts()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(DevLogLevel.Error));
        sut.Add(CreateLogEntry(DevLogLevel.Warning));

        // Act
        sut.Clear();

        // Assert
        var stats = sut.GetStats();
        stats.EntriesByLevel.Values.Should().AllBeEquivalentTo(0);
    }

    [Fact]
    public void Clear_ShouldResetMemoryUsage()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(message: "Some long message"));

        // Act
        sut.Clear();

        // Assert
        var stats = sut.GetStats();
        stats.MemoryUsageBytes.Should().Be(0);
    }

    [Fact]
    public void Clear_ShouldClearErrorClusters()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(DevLogLevel.Error, "Error message"));

        // Act
        sut.Clear();

        // Assert
        var clusters = sut.GetErrorClusters();
        clusters.Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldAllowNewEntriesToBeAdded()
    {
        // Arrange
        var sut = CreateSut();
        sut.Add(CreateLogEntry(message: "Before clear"));
        sut.Clear();

        // Act
        sut.Add(CreateLogEntry(message: "After clear"));

        // Assert
        var stats = sut.GetStats();
        stats.TotalEntries.Should().Be(1);

        var entries = sut.GetRecentEntries(10).ToList();
        entries[0].Message.Should().Be("After clear");
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Add_ShouldBeThreadSafe()
    {
        // Arrange
        var sut = CreateSut();
        var tasks = new List<Task>();

        // Act - Add entries from multiple threads
        for (int i = 0; i < 10; i++)
        {
            var threadIndex = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    sut.Add(CreateLogEntry(message: $"Thread {threadIndex} Entry {j}"));
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var stats = sut.GetStats();
        stats.TotalEntries.Should().Be(100);
    }

    [Fact]
    public async Task GetRecentEntries_ShouldBeThreadSafe()
    {
        // Arrange
        var sut = CreateSut();
        for (int i = 0; i < 50; i++)
        {
            sut.Add(CreateLogEntry(message: $"Entry {i}"));
        }

        var tasks = new List<Task<IEnumerable<LogEntryDto>>>();

        // Act - Read from multiple threads while adding
        var addTask = Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                sut.Add(CreateLogEntry(message: $"New Entry {i}"));
            }
        });

        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(() => sut.GetRecentEntries(20)));
        }

        await Task.WhenAll(tasks);
        await addTask;

        // Assert - No exceptions should occur
        foreach (var task in tasks)
        {
            var result = await task;
            result.Should().NotBeNull();
        }
    }

    #endregion
}
