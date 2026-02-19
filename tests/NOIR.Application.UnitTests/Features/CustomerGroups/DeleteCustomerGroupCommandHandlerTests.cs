using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Models;
using NOIR.Application.Features.CustomerGroups.Commands.DeleteCustomerGroup;
using NOIR.Application.Features.CustomerGroups.Specifications;
using NOIR.Domain.Common;
using NOIR.Domain.Entities.Customer;

namespace NOIR.Application.UnitTests.Features.CustomerGroups;

/// <summary>
/// Unit tests for DeleteCustomerGroupCommandHandler.
/// </summary>
public class DeleteCustomerGroupCommandHandlerTests
{
    private readonly Mock<IRepository<CustomerGroup, Guid>> _groupRepositoryMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeleteCustomerGroupCommandHandler _handler;

    public DeleteCustomerGroupCommandHandlerTests()
    {
        _groupRepositoryMock = new Mock<IRepository<CustomerGroup, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteCustomerGroupCommandHandler(
            _groupRepositoryMock.Object,
            _dbContextMock.Object,
            _unitOfWorkMock.Object);
    }

    private static CustomerGroup CreateTestGroup()
    {
        return CustomerGroup.Create("VIP Customers", "Top-tier customers", "tenant-123");
    }

    #region Success Scenarios

    [Fact]
    public async Task Handle_GroupExistsWithNoMembers_SoftDeletesGroup()
    {
        // Arrange
        var group = CreateTestGroup();
        var command = new DeleteCustomerGroupCommand(group.Id) { UserId = "user-1" };

        _groupRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        // Mock empty memberships query
        var emptyMemberships = new List<CustomerGroupMembership>().AsQueryable();
        var mockDbSet = new Mock<DbSet<CustomerGroupMembership>>();
        mockDbSet.As<IQueryable<CustomerGroupMembership>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<CustomerGroupMembership>(emptyMemberships.Provider));
        mockDbSet.As<IQueryable<CustomerGroupMembership>>().Setup(m => m.Expression).Returns(emptyMemberships.Expression);
        mockDbSet.As<IQueryable<CustomerGroupMembership>>().Setup(m => m.ElementType).Returns(emptyMemberships.ElementType);
        mockDbSet.As<IQueryable<CustomerGroupMembership>>().Setup(m => m.GetEnumerator()).Returns(emptyMemberships.GetEnumerator());
        _dbContextMock.Setup(x => x.CustomerGroupMemberships).Returns(mockDbSet.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _groupRepositoryMock.Verify(x => x.Remove(It.IsAny<CustomerGroup>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_GroupNotFound_ReturnsNotFound()
    {
        // Arrange
        var command = new DeleteCustomerGroupCommand(Guid.NewGuid()) { UserId = "user-1" };

        _groupRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerGroup?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.CustomerGroup.NotFound);
    }

    #endregion
}

// Helper for async queryable mocking
internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: [typeof(Expression)])!
            .MakeGenericMethod(resultType)
            .Invoke(this, [expression]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [executionResult])!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable) { }

    public TestAsyncEnumerable(Expression expression)
        : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(_inner.MoveNext());
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return default;
    }
}
