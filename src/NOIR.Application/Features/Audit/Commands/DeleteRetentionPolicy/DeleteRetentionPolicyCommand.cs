using NOIR.Application.Features.Audit.Specifications;

namespace NOIR.Application.Features.Audit.Commands.DeleteRetentionPolicy;

/// <summary>
/// Command to delete an audit retention policy.
/// </summary>
public record DeleteRetentionPolicyCommand(Guid Id) : IAuditableCommand
{
    public object? GetTargetId() => Id;
    public AuditOperationType OperationType => AuditOperationType.Delete;
}

/// <summary>
/// Handler for DeleteRetentionPolicyCommand.
/// </summary>
public class DeleteRetentionPolicyCommandHandler
{
    private readonly IRepository<AuditRetentionPolicy, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRetentionPolicyCommandHandler(
        IRepository<AuditRetentionPolicy, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteRetentionPolicyCommand cmd,
        CancellationToken ct)
    {
        var spec = new AuditRetentionPolicyByIdTrackingSpec(cmd.Id);
        var policy = await _repository.FirstOrDefaultAsync(spec, ct);

        if (policy is null)
        {
            return Result.Failure(
                Error.NotFound("RetentionPolicy.NotFound", $"Retention policy with ID {cmd.Id} not found"));
        }

        // Prevent deleting the system default policy
        if (policy.TenantId is null && policy.Name == "System Default")
        {
            return Result.Failure(
                Error.Conflict("RetentionPolicy.SystemDefault", "Cannot delete the system default retention policy"));
        }

        _repository.Remove(policy);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}

/// <summary>
/// Validator for DeleteRetentionPolicyCommand.
/// </summary>
public class DeleteRetentionPolicyCommandValidator : AbstractValidator<DeleteRetentionPolicyCommand>
{
    public DeleteRetentionPolicyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Policy ID is required");
    }
}
