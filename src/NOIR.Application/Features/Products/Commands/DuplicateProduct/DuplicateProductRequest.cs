namespace NOIR.Application.Features.Products.Commands.DuplicateProduct;

/// <summary>
/// Request body for duplicate product endpoint.
/// </summary>
public sealed record DuplicateProductRequest(
    bool CopyVariants = false,
    bool CopyImages = false,
    bool CopyOptions = false);
