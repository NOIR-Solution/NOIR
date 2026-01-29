namespace NOIR.Application.Features.ProductAttributes.Commands.DeleteProductAttribute;

/// <summary>
/// Wolverine handler for deleting a product attribute.
/// </summary>
public class DeleteProductAttributeCommandHandler
{
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductAttributeCommandHandler(
        IRepository<ProductAttribute, Guid> attributeRepository,
        IUnitOfWork unitOfWork)
    {
        _attributeRepository = attributeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        DeleteProductAttributeCommand command,
        CancellationToken cancellationToken)
    {
        // Find the attribute with tracking enabled for modification
        var spec = new ProductAttributeByIdForUpdateSpec(command.Id);
        var attribute = await _attributeRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (attribute == null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Product attribute with ID '{command.Id}' not found.", ErrorCodes.Attribute.NotFound));
        }

        // Set the name for audit logging
        command.AttributeName = attribute.Name;

        // Mark as deleted (domain event)
        attribute.MarkAsDeleted();

        // Soft delete via repository (handled by interceptor)
        _attributeRepository.Remove(attribute);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
