namespace NOIR.Domain.Entities.Crm;

/// <summary>
/// A sales lead/deal in the CRM pipeline.
/// </summary>
public class Lead : TenantAggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public Guid ContactId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public decimal Value { get; private set; }
    public string Currency { get; private set; } = "USD";
    public Guid? OwnerId { get; private set; }
    public Guid PipelineId { get; private set; }
    public Guid StageId { get; private set; }
    public LeadStatus Status { get; private set; }
    public double SortOrder { get; private set; }
    public DateTimeOffset? ExpectedCloseDate { get; private set; }
    public DateTimeOffset? WonAt { get; private set; }
    public DateTimeOffset? LostAt { get; private set; }
    public string? LostReason { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual CrmContact? Contact { get; private set; }
    public virtual CrmCompany? Company { get; private set; }
    public virtual Employee? Owner { get; private set; }
    public virtual Pipeline? Pipeline { get; private set; }
    public virtual PipelineStage? Stage { get; private set; }

    // Private constructor for EF Core
    private Lead() : base() { }

    private Lead(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new lead.
    /// </summary>
    public static Lead Create(
        string title,
        Guid contactId,
        Guid pipelineId,
        Guid stageId,
        string? tenantId,
        Guid? companyId = null,
        decimal value = 0,
        string currency = "USD",
        Guid? ownerId = null,
        double sortOrder = 0,
        DateTimeOffset? expectedCloseDate = null,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var lead = new Lead(Guid.NewGuid(), tenantId)
        {
            Title = title.Trim(),
            ContactId = contactId,
            PipelineId = pipelineId,
            StageId = stageId,
            CompanyId = companyId,
            Value = value,
            Currency = currency,
            OwnerId = ownerId,
            Status = LeadStatus.Active,
            SortOrder = sortOrder,
            ExpectedCloseDate = expectedCloseDate,
            Notes = notes?.Trim()
        };

        lead.AddDomainEvent(new Events.Crm.LeadCreatedEvent(lead.Id));
        return lead;
    }

    /// <summary>
    /// Updates lead information.
    /// </summary>
    public void Update(
        string title,
        Guid contactId,
        Guid? companyId,
        decimal value,
        string currency,
        Guid? ownerId,
        DateTimeOffset? expectedCloseDate,
        string? notes)
    {
        Title = title.Trim();
        ContactId = contactId;
        CompanyId = companyId;
        Value = value;
        Currency = currency;
        OwnerId = ownerId;
        ExpectedCloseDate = expectedCloseDate;
        Notes = notes?.Trim();
    }

    /// <summary>
    /// Moves the lead to a different stage in the pipeline.
    /// </summary>
    public void MoveToStage(Guid stageId, double sortOrder)
    {
        StageId = stageId;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Marks the lead as won.
    /// </summary>
    public void Win()
    {
        if (Status != LeadStatus.Active)
        {
            throw new InvalidOperationException("Only active leads can be won.");
        }

        Status = LeadStatus.Won;
        WonAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.Crm.LeadWonEvent(Id, ContactId, null));
    }

    /// <summary>
    /// Marks the lead as lost.
    /// </summary>
    public void Lose(string? reason)
    {
        if (Status != LeadStatus.Active)
        {
            throw new InvalidOperationException("Only active leads can be lost.");
        }

        Status = LeadStatus.Lost;
        LostAt = DateTimeOffset.UtcNow;
        LostReason = reason?.Trim();
        AddDomainEvent(new Events.Crm.LeadLostEvent(Id, reason));
    }

    /// <summary>
    /// Reopens a won or lost lead back to active status.
    /// </summary>
    public void Reopen()
    {
        if (Status == LeadStatus.Active)
        {
            throw new InvalidOperationException("Lead is already active.");
        }

        Status = LeadStatus.Active;
        WonAt = null;
        LostAt = null;
        LostReason = null;
        AddDomainEvent(new Events.Crm.LeadReopenedEvent(Id));
    }
}
