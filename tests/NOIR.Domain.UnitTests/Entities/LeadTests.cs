using NOIR.Domain.Entities.Crm;

namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the Lead entity.
/// Tests factory methods, stage movement, and win/lose/reopen lifecycle.
/// </summary>
public class LeadTests
{
    private const string TestTenantId = "test-tenant";

    private static Lead CreateActiveLead() =>
        Lead.Create("Test Deal", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            TestTenantId, value: 10000m, currency: "USD");

    #region Create Factory Tests

    [Fact]
    public void Create_ShouldSetAllProperties_AndStatusActive()
    {
        // Arrange
        var title = "Enterprise Deal";
        var contactId = Guid.NewGuid();
        var pipelineId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        // Act
        var lead = Lead.Create(
            title, contactId, pipelineId, stageId, TestTenantId,
            companyId, 50000m, "EUR", ownerId, 1.5,
            DateTimeOffset.UtcNow.AddDays(30), "Big deal");

        // Assert
        lead.ShouldNotBeNull();
        lead.Id.ShouldNotBe(Guid.Empty);
        lead.Title.ShouldBe("Enterprise Deal");
        lead.ContactId.ShouldBe(contactId);
        lead.PipelineId.ShouldBe(pipelineId);
        lead.StageId.ShouldBe(stageId);
        lead.CompanyId.ShouldBe(companyId);
        lead.Value.ShouldBe(50000m);
        lead.Currency.ShouldBe("EUR");
        lead.OwnerId.ShouldBe(ownerId);
        lead.Status.ShouldBe(LeadStatus.Active);
        lead.SortOrder.ShouldBe(1.5);
        lead.ExpectedCloseDate.ShouldNotBeNull();
        lead.Notes.ShouldBe("Big deal");
        lead.TenantId.ShouldBe(TestTenantId);
        lead.WonAt.ShouldBeNull();
        lead.LostAt.ShouldBeNull();
        lead.LostReason.ShouldBeNull();
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldThrow()
    {
        // Act & Assert
        var act = () => Lead.Create(
            "", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TestTenantId);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithDefaults_ShouldUseDefaultValues()
    {
        // Act
        var lead = Lead.Create(
            "Simple Deal", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TestTenantId);

        // Assert
        lead.Value.ShouldBe(0m);
        lead.Currency.ShouldBe("USD");
        lead.SortOrder.ShouldBe(0);
        lead.CompanyId.ShouldBeNull();
        lead.OwnerId.ShouldBeNull();
        lead.ExpectedCloseDate.ShouldBeNull();
        lead.Notes.ShouldBeNull();
    }

    #endregion

    #region MoveToStage Tests

    [Fact]
    public void MoveToStage_ShouldUpdateStageAndSortOrder()
    {
        // Arrange
        var lead = CreateActiveLead();
        var newStageId = Guid.NewGuid();
        var newSortOrder = 2.5;

        // Act
        lead.MoveToStage(newStageId, newSortOrder);

        // Assert
        lead.StageId.ShouldBe(newStageId);
        lead.SortOrder.ShouldBe(2.5);
    }

    #endregion

    #region Win Tests

    [Fact]
    public void Win_ShouldSetStatusWon_AndWonAt()
    {
        // Arrange
        var lead = CreateActiveLead();
        var beforeWin = DateTimeOffset.UtcNow;

        // Act
        lead.Win();

        // Assert
        lead.Status.ShouldBe(LeadStatus.Won);
        lead.WonAt.ShouldNotBeNull();
        lead.WonAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeWin);
    }

    [Fact]
    public void Win_WhenNotActive_ShouldThrow()
    {
        // Arrange
        var lead = CreateActiveLead();
        lead.Win(); // Now Won

        // Act & Assert
        var act = () => lead.Win();
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Only active leads can be won.");
    }

    [Fact]
    public void Win_WhenLost_ShouldThrow()
    {
        // Arrange
        var lead = CreateActiveLead();
        lead.Lose("Not interested");

        // Act & Assert
        var act = () => lead.Win();
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Only active leads can be won.");
    }

    #endregion

    #region Lose Tests

    [Fact]
    public void Lose_ShouldSetStatusLost_AndLostReason()
    {
        // Arrange
        var lead = CreateActiveLead();
        var reason = "Budget constraints";
        var beforeLose = DateTimeOffset.UtcNow;

        // Act
        lead.Lose(reason);

        // Assert
        lead.Status.ShouldBe(LeadStatus.Lost);
        lead.LostAt.ShouldNotBeNull();
        lead.LostAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeLose);
        lead.LostReason.ShouldBe("Budget constraints");
    }

    [Fact]
    public void Lose_WithNullReason_ShouldAllowNull()
    {
        // Arrange
        var lead = CreateActiveLead();

        // Act
        lead.Lose(null);

        // Assert
        lead.Status.ShouldBe(LeadStatus.Lost);
        lead.LostReason.ShouldBeNull();
    }

    [Fact]
    public void Lose_WhenNotActive_ShouldThrow()
    {
        // Arrange
        var lead = CreateActiveLead();
        lead.Lose("Already lost");

        // Act & Assert
        var act = () => lead.Lose("Again");
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Only active leads can be lost.");
    }

    [Fact]
    public void Lose_WhenWon_ShouldThrow()
    {
        // Arrange
        var lead = CreateActiveLead();
        lead.Win();

        // Act & Assert
        var act = () => lead.Lose("Changed mind");
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Only active leads can be lost.");
    }

    #endregion

    #region Reopen Tests

    [Fact]
    public void Reopen_WhenWon_ShouldResetToActive()
    {
        // Arrange
        var lead = CreateActiveLead();
        lead.Win();

        // Act
        lead.Reopen();

        // Assert
        lead.Status.ShouldBe(LeadStatus.Active);
        lead.WonAt.ShouldBeNull();
        lead.LostAt.ShouldBeNull();
        lead.LostReason.ShouldBeNull();
    }

    [Fact]
    public void Reopen_WhenLost_ShouldResetToActive()
    {
        // Arrange
        var lead = CreateActiveLead();
        lead.Lose("Budget");

        // Act
        lead.Reopen();

        // Assert
        lead.Status.ShouldBe(LeadStatus.Active);
        lead.WonAt.ShouldBeNull();
        lead.LostAt.ShouldBeNull();
        lead.LostReason.ShouldBeNull();
    }

    [Fact]
    public void Reopen_WhenActive_ShouldThrow()
    {
        // Arrange
        var lead = CreateActiveLead();

        // Act & Assert
        var act = () => lead.Reopen();
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Lead is already active.");
    }

    #endregion

    #region Domain Event Tests

    [Fact]
    public void Win_ShouldRaiseLeadWonEvent()
    {
        // Arrange
        var lead = CreateActiveLead();

        // Act
        lead.Win();

        // Assert
        lead.DomainEvents.ShouldContain(e => e is Events.Crm.LeadWonEvent);
    }

    [Fact]
    public void Lose_ShouldRaiseLeadLostEvent()
    {
        // Arrange
        var lead = CreateActiveLead();

        // Act
        lead.Lose("Lost reason");

        // Assert
        lead.DomainEvents.ShouldContain(e => e is Events.Crm.LeadLostEvent);
    }

    [Fact]
    public void Reopen_ShouldRaiseLeadReopenedEvent()
    {
        // Arrange
        var lead = CreateActiveLead();
        lead.Win();

        // Act
        lead.Reopen();

        // Assert
        lead.DomainEvents.ShouldContain(e => e is Events.Crm.LeadReopenedEvent);
    }

    #endregion
}
