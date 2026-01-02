namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for BackgroundJobsService.
/// Tests the Hangfire wrapper methods for scheduling background jobs.
/// Note: These tests verify the service API contract, not actual Hangfire behavior,
/// since Hangfire requires a server context which isn't available in unit tests.
/// </summary>
public class BackgroundJobsServiceTests
{
    private readonly BackgroundJobsService _sut;

    public BackgroundJobsServiceTests()
    {
        _sut = new BackgroundJobsService();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Act
        var service = new BackgroundJobsService();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region Enqueue Method Existence Tests

    [Fact]
    public void Enqueue_ActionOverload_MethodShouldExist()
    {
        // Assert - Verify the method exists with correct signature
        var method = typeof(BackgroundJobsService)
            .GetMethod("Enqueue", [typeof(Expression<Action>)]);

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    [Fact]
    public void Enqueue_FuncTaskOverload_MethodShouldExist()
    {
        // Assert
        var method = typeof(BackgroundJobsService)
            .GetMethod("Enqueue", [typeof(Expression<Func<Task>>)]);

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    [Fact]
    public void Enqueue_GenericAction_MethodShouldExist()
    {
        // Assert - Check for generic version
        var methods = typeof(BackgroundJobsService)
            .GetMethods()
            .Where(m => m.Name == "Enqueue" && m.IsGenericMethod);

        methods.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Schedule Method Existence Tests

    [Fact]
    public void Schedule_ActionWithTimeSpan_MethodShouldExist()
    {
        // Assert
        var method = typeof(BackgroundJobsService)
            .GetMethod("Schedule", [
                typeof(Expression<Action>),
                typeof(TimeSpan)
            ]);

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    [Fact]
    public void Schedule_FuncTaskWithTimeSpan_MethodShouldExist()
    {
        // Assert
        var method = typeof(BackgroundJobsService)
            .GetMethod("Schedule", [
                typeof(Expression<Func<Task>>),
                typeof(TimeSpan)
            ]);

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    [Fact]
    public void Schedule_ActionWithDateTimeOffset_MethodShouldExist()
    {
        // Assert
        var method = typeof(BackgroundJobsService)
            .GetMethod("Schedule", [
                typeof(Expression<Action>),
                typeof(DateTimeOffset)
            ]);

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    [Fact]
    public void Schedule_FuncTaskWithDateTimeOffset_MethodShouldExist()
    {
        // Assert
        var method = typeof(BackgroundJobsService)
            .GetMethod("Schedule", [
                typeof(Expression<Func<Task>>),
                typeof(DateTimeOffset)
            ]);

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    #endregion

    #region RecurringJob Method Existence Tests

    [Fact]
    public void RecurringJob_ActionOverload_MethodShouldExist()
    {
        // Assert
        var method = typeof(BackgroundJobsService)
            .GetMethod("RecurringJob", [
                typeof(string),
                typeof(Expression<Action>),
                typeof(string)
            ]);

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void RecurringJob_FuncTaskOverload_MethodShouldExist()
    {
        // Assert
        var method = typeof(BackgroundJobsService)
            .GetMethod("RecurringJob", [
                typeof(string),
                typeof(Expression<Func<Task>>),
                typeof(string)
            ]);

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void RecurringJob_GenericOverloads_ShouldExist()
    {
        // Assert
        var methods = typeof(BackgroundJobsService)
            .GetMethods()
            .Where(m => m.Name == "RecurringJob" && m.IsGenericMethod);

        methods.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    #endregion

    #region RemoveRecurringJob Method Tests

    [Fact]
    public void RemoveRecurringJob_MethodShouldExist()
    {
        // Assert
        var method = typeof(BackgroundJobsService)
            .GetMethod("RemoveRecurringJob", [typeof(string)]);

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    #endregion

    #region ContinueWith Method Existence Tests

    [Fact]
    public void ContinueWith_ActionOverload_MethodShouldExist()
    {
        // Assert
        var method = typeof(BackgroundJobsService)
            .GetMethod("ContinueWith", [
                typeof(string),
                typeof(Expression<Action>)
            ]);

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    [Fact]
    public void ContinueWith_FuncTaskOverload_MethodShouldExist()
    {
        // Assert
        var method = typeof(BackgroundJobsService)
            .GetMethod("ContinueWith", [
                typeof(string),
                typeof(Expression<Func<Task>>)
            ]);

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void Service_ShouldImplementIBackgroundJobs()
    {
        // Assert
        _sut.Should().BeAssignableTo<IBackgroundJobs>();
    }

    [Fact]
    public void Service_ShouldImplementIScopedService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IScopedService>();
    }

    #endregion

    #region Method Count Tests

    [Fact]
    public void EnqueueMethods_ShouldHave4Overloads()
    {
        // Assert - 4 overloads: Action, Func<Task>, Action<T>, Func<T, Task>
        var methods = typeof(BackgroundJobsService)
            .GetMethods()
            .Where(m => m.Name == "Enqueue");

        methods.Should().HaveCount(4);
    }

    [Fact]
    public void ScheduleMethods_ShouldHave4Overloads()
    {
        // Assert - 4 overloads: Action+TimeSpan, Func<Task>+TimeSpan, Action+DateTimeOffset, Func<Task>+DateTimeOffset
        var methods = typeof(BackgroundJobsService)
            .GetMethods()
            .Where(m => m.Name == "Schedule");

        methods.Should().HaveCount(4);
    }

    [Fact]
    public void RecurringJobMethods_ShouldHave4Overloads()
    {
        // Assert - 4 overloads: Action, Func<Task>, Action<T>, Func<T, Task>
        var methods = typeof(BackgroundJobsService)
            .GetMethods()
            .Where(m => m.Name == "RecurringJob");

        methods.Should().HaveCount(4);
    }

    [Fact]
    public void ContinueWithMethods_ShouldHave2Overloads()
    {
        // Assert - 2 overloads: Action, Func<Task>
        var methods = typeof(BackgroundJobsService)
            .GetMethods()
            .Where(m => m.Name == "ContinueWith");

        methods.Should().HaveCount(2);
    }

    #endregion
}
