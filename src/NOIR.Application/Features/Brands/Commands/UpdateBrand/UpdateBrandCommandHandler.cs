namespace NOIR.Application.Features.Brands.Commands.UpdateBrand;

/// <summary>
/// Wolverine handler for updating a brand.
/// </summary>
public class UpdateBrandCommandHandler
{
    private readonly IRepository<Brand, Guid> _brandRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public UpdateBrandCommandHandler(
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
        UpdateBrandCommand command,
        CancellationToken cancellationToken)
    {
        // Get the brand for update
        var spec = new BrandByIdForUpdateSpec(command.Id);
        var brand = await _brandRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (brand == null)
        {
            return Result.Failure<BrandDto>(
                Error.NotFound($"Brand with ID '{command.Id}' was not found.", ErrorCodes.Brand.NotFound));
        }

        // Check if slug already exists (for different brand)
        if (brand.Slug != command.Slug)
        {
            var slugSpec = new BrandSlugExistsSpec(command.Slug, command.Id);
            var existingBrand = await _brandRepository.FirstOrDefaultAsync(slugSpec, cancellationToken);
            if (existingBrand != null)
            {
                return Result.Failure<BrandDto>(
                    Error.Conflict($"A brand with slug '{command.Slug}' already exists.", ErrorCodes.Brand.DuplicateSlug));
            }
        }

        // Update the brand
        brand.UpdateDetails(command.Name, command.Slug, command.Description, command.Website);
        brand.UpdateBranding(command.LogoUrl, command.BannerUrl);
        brand.UpdateSeo(command.MetaTitle, command.MetaDescription);
        brand.SetActive(command.IsActive);
        brand.SetFeatured(command.IsFeatured);
        brand.SetSortOrder(command.SortOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Brand",
            entityId: brand.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(BrandMapper.ToDto(brand));
    }
}
