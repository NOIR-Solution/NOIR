namespace NOIR.Application.Features.Brands.Commands.CreateBrand;

/// <summary>
/// Wolverine handler for creating a new brand.
/// </summary>
public class CreateBrandCommandHandler
{
    private readonly IRepository<Brand, Guid> _brandRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreateBrandCommandHandler(
        IRepository<Brand, Guid> brandRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _brandRepository = brandRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<BrandDto>> Handle(
        CreateBrandCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Check if slug already exists
        var slugSpec = new BrandSlugExistsSpec(command.Slug);
        var existingBrand = await _brandRepository.FirstOrDefaultAsync(slugSpec, cancellationToken);
        if (existingBrand != null)
        {
            return Result.Failure<BrandDto>(
                Error.Conflict($"A brand with slug '{command.Slug}' already exists.", ErrorCodes.Brand.DuplicateSlug));
        }

        // Create the brand
        var brand = Brand.Create(command.Name, command.Slug, tenantId);

        // Set additional properties
        if (!string.IsNullOrEmpty(command.Description) || !string.IsNullOrEmpty(command.Website))
        {
            brand.UpdateDetails(command.Name, command.Slug, command.Description, command.Website);
        }

        brand.UpdateBranding(command.LogoUrl, command.BannerUrl);
        brand.UpdateSeo(command.MetaTitle, command.MetaDescription);

        if (command.IsFeatured)
        {
            brand.SetFeatured(true);
        }

        await _brandRepository.AddAsync(brand, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Brand",
            entityId: brand.Id,
            operation: EntityOperation.Created,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(BrandMapper.ToDto(brand));
    }
}
