namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for StringLengthByNameConvention.
/// Tests that string properties get correct max lengths based on naming conventions.
/// </summary>
public class StringLengthByNameConventionTests
{
    #region Test Entities

    private class TestEntityWithNames
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Title { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Phone { get; set; }
    }

    private class TestEntityWithContent
    {
        public Guid Id { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }
        public string? Comment { get; set; }
        public string? Message { get; set; }
    }

    private class TestEntityWithReferences
    {
        public Guid Id { get; set; }
        public string? Code { get; set; }
        public string? Reference { get; set; }
        public string? ExternalId { get; set; }
    }

    private class TestEntityWithAddress
    {
        public Guid Id { get; set; }
        public string? Address { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string? ZipCode { get; set; }
    }

    private class TestEntityWithUrls
    {
        public Guid Id { get; set; }
        public string? Url { get; set; }
        public string? ImageUrl { get; set; }
        public string? Website { get; set; }
    }

    private class TestEntityWithNetwork
    {
        public Guid Id { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? DeviceName { get; set; }
    }

    private class TestEntityWithSecurity
    {
        public Guid Id { get; set; }
        public string? Token { get; set; }
        public string? CorrelationId { get; set; }
    }

    private class TestEntityWithSuffixes
    {
        public Guid Id { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? ShippingAddress { get; set; }
    }

    private class TestEntityWithPresetLength
    {
        public Guid Id { get; set; }
        public string? Name { get; set; } // Will have preset max length
        public string? Description { get; set; } // Will have preset max length
    }

    private class TestEntityWithNoMatch
    {
        public Guid Id { get; set; }
        public string? SomeRandomField { get; set; }
        public string? AnotherField { get; set; }
    }

    private class TestDbContext : DbContext
    {
        public DbSet<TestEntityWithNames> TestEntitiesWithNames => Set<TestEntityWithNames>();
        public DbSet<TestEntityWithContent> TestEntitiesWithContent => Set<TestEntityWithContent>();
        public DbSet<TestEntityWithReferences> TestEntitiesWithReferences => Set<TestEntityWithReferences>();
        public DbSet<TestEntityWithAddress> TestEntitiesWithAddress => Set<TestEntityWithAddress>();
        public DbSet<TestEntityWithUrls> TestEntitiesWithUrls => Set<TestEntityWithUrls>();
        public DbSet<TestEntityWithNetwork> TestEntitiesWithNetwork => Set<TestEntityWithNetwork>();
        public DbSet<TestEntityWithSecurity> TestEntitiesWithSecurity => Set<TestEntityWithSecurity>();
        public DbSet<TestEntityWithSuffixes> TestEntitiesWithSuffixes => Set<TestEntityWithSuffixes>();
        public DbSet<TestEntityWithPresetLength> TestEntitiesWithPresetLength => Set<TestEntityWithPresetLength>();
        public DbSet<TestEntityWithNoMatch> TestEntitiesWithNoMatch => Set<TestEntityWithNoMatch>();

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Conventions.Add(_ => new StringLengthByNameConvention());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Set preset lengths for some properties to test that they're not overwritten
            modelBuilder.Entity<TestEntityWithPresetLength>()
                .Property(e => e.Name).HasMaxLength(50);
            modelBuilder.Entity<TestEntityWithPresetLength>()
                .Property(e => e.Description).HasMaxLength(100);
        }
    }

    #endregion

    #region Helper Methods

    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    private static int? GetMaxLength<TEntity>(DbContext context, string propertyName) where TEntity : class
    {
        return context.Model.FindEntityType(typeof(TEntity))
            ?.FindProperty(propertyName)
            ?.GetMaxLength();
    }

    #endregion

    #region Identity Fields Tests

    [Fact]
    public void Convention_ShouldSetNameMaxLength_To200()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithNames>(context, nameof(TestEntityWithNames.Name));
        maxLength.Should().Be(200);
    }

    [Fact]
    public void Convention_ShouldSetTitleMaxLength_To200()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithNames>(context, nameof(TestEntityWithNames.Title));
        maxLength.Should().Be(200);
    }

    [Fact]
    public void Convention_ShouldSetEmailMaxLength_To256()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithNames>(context, nameof(TestEntityWithNames.Email));
        maxLength.Should().Be(256);
    }

    [Fact]
    public void Convention_ShouldSetPhoneNumberMaxLength_To20()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithNames>(context, nameof(TestEntityWithNames.PhoneNumber));
        maxLength.Should().Be(20);
    }

    [Fact]
    public void Convention_ShouldSetPhoneMaxLength_To20()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithNames>(context, nameof(TestEntityWithNames.Phone));
        maxLength.Should().Be(20);
    }

    #endregion

    #region Content Fields Tests

    [Fact]
    public void Convention_ShouldSetDescriptionMaxLength_To2000()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithContent>(context, nameof(TestEntityWithContent.Description));
        maxLength.Should().Be(2000);
    }

    [Fact]
    public void Convention_ShouldSetNotesMaxLength_To4000()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithContent>(context, nameof(TestEntityWithContent.Notes));
        maxLength.Should().Be(4000);
    }

    [Fact]
    public void Convention_ShouldSetCommentMaxLength_To4000()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithContent>(context, nameof(TestEntityWithContent.Comment));
        maxLength.Should().Be(4000);
    }

    [Fact]
    public void Convention_ShouldSetMessageMaxLength_To4000()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithContent>(context, nameof(TestEntityWithContent.Message));
        maxLength.Should().Be(4000);
    }

    #endregion

    #region Reference Fields Tests

    [Fact]
    public void Convention_ShouldSetCodeMaxLength_To50()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithReferences>(context, nameof(TestEntityWithReferences.Code));
        maxLength.Should().Be(50);
    }

    [Fact]
    public void Convention_ShouldSetReferenceMaxLength_To100()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithReferences>(context, nameof(TestEntityWithReferences.Reference));
        maxLength.Should().Be(100);
    }

    [Fact]
    public void Convention_ShouldSetExternalIdMaxLength_To100()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithReferences>(context, nameof(TestEntityWithReferences.ExternalId));
        maxLength.Should().Be(100);
    }

    #endregion

    #region Address Fields Tests

    [Fact]
    public void Convention_ShouldSetAddressMaxLength_To500()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithAddress>(context, nameof(TestEntityWithAddress.Address));
        maxLength.Should().Be(500);
    }

    [Fact]
    public void Convention_ShouldSetStreetMaxLength_To200()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithAddress>(context, nameof(TestEntityWithAddress.Street));
        maxLength.Should().Be(200);
    }

    [Fact]
    public void Convention_ShouldSetCityMaxLength_To100()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithAddress>(context, nameof(TestEntityWithAddress.City));
        maxLength.Should().Be(100);
    }

    [Fact]
    public void Convention_ShouldSetStateMaxLength_To100()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithAddress>(context, nameof(TestEntityWithAddress.State));
        maxLength.Should().Be(100);
    }

    [Fact]
    public void Convention_ShouldSetCountryMaxLength_To100()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithAddress>(context, nameof(TestEntityWithAddress.Country));
        maxLength.Should().Be(100);
    }

    [Fact]
    public void Convention_ShouldSetPostalCodeMaxLength_To50()
    {
        // PostalCode ends with "Code" which matches earlier in dictionary iteration
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithAddress>(context, nameof(TestEntityWithAddress.PostalCode));
        maxLength.Should().Be(50);
    }

    [Fact]
    public void Convention_ShouldSetZipCodeMaxLength_To50()
    {
        // ZipCode ends with "Code" which matches earlier in dictionary iteration
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithAddress>(context, nameof(TestEntityWithAddress.ZipCode));
        maxLength.Should().Be(50);
    }

    #endregion

    #region URL Fields Tests

    [Fact]
    public void Convention_ShouldSetUrlMaxLength_To2000()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithUrls>(context, nameof(TestEntityWithUrls.Url));
        maxLength.Should().Be(2000);
    }

    [Fact]
    public void Convention_ShouldSetImageUrlMaxLength_To2000()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithUrls>(context, nameof(TestEntityWithUrls.ImageUrl));
        maxLength.Should().Be(2000);
    }

    [Fact]
    public void Convention_ShouldSetWebsiteMaxLength_To2000()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithUrls>(context, nameof(TestEntityWithUrls.Website));
        maxLength.Should().Be(2000);
    }

    #endregion

    #region Network Fields Tests

    [Fact]
    public void Convention_ShouldSetIpAddressMaxLength_To500()
    {
        // IpAddress ends with "Address" which matches earlier in dictionary iteration
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithNetwork>(context, nameof(TestEntityWithNetwork.IpAddress));
        maxLength.Should().Be(500);
    }

    [Fact]
    public void Convention_ShouldSetUserAgentMaxLength_To500()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithNetwork>(context, nameof(TestEntityWithNetwork.UserAgent));
        maxLength.Should().Be(500);
    }

    [Fact]
    public void Convention_ShouldSetDeviceNameMaxLength_To200()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithNetwork>(context, nameof(TestEntityWithNetwork.DeviceName));
        maxLength.Should().Be(200);
    }

    #endregion

    #region Security Fields Tests

    [Fact]
    public void Convention_ShouldSetTokenMaxLength_To500()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithSecurity>(context, nameof(TestEntityWithSecurity.Token));
        maxLength.Should().Be(500);
    }

    [Fact]
    public void Convention_ShouldSetCorrelationIdMaxLength_To100()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithSecurity>(context, nameof(TestEntityWithSecurity.CorrelationId));
        maxLength.Should().Be(100);
    }

    #endregion

    #region Suffix Matching Tests

    [Fact]
    public void Convention_ShouldMatchPropertiesEndingWithName()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithSuffixes>(context, nameof(TestEntityWithSuffixes.CustomerName));
        maxLength.Should().Be(200);
    }

    [Fact]
    public void Convention_ShouldMatchPropertiesEndingWithEmail()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithSuffixes>(context, nameof(TestEntityWithSuffixes.CustomerEmail));
        maxLength.Should().Be(256);
    }

    [Fact]
    public void Convention_ShouldMatchPropertiesEndingWithUrl()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithSuffixes>(context, nameof(TestEntityWithSuffixes.ProfileImageUrl));
        maxLength.Should().Be(2000);
    }

    [Fact]
    public void Convention_ShouldMatchPropertiesEndingWithAddress()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithSuffixes>(context, nameof(TestEntityWithSuffixes.ShippingAddress));
        maxLength.Should().Be(500);
    }

    #endregion

    #region Preset Length Tests

    [Fact]
    public void Convention_ShouldNotOverridePresetMaxLength_ForName()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithPresetLength>(context, nameof(TestEntityWithPresetLength.Name));
        maxLength.Should().Be(50); // Preset, not 200
    }

    [Fact]
    public void Convention_ShouldNotOverridePresetMaxLength_ForDescription()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithPresetLength>(context, nameof(TestEntityWithPresetLength.Description));
        maxLength.Should().Be(100); // Preset, not 2000
    }

    #endregion

    #region No Match Tests

    [Fact]
    public void Convention_ShouldNotSetMaxLength_ForUnrecognizedPropertyNames()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithNoMatch>(context, nameof(TestEntityWithNoMatch.SomeRandomField));
        maxLength.Should().BeNull();
    }

    [Fact]
    public void Convention_ShouldNotSetMaxLength_ForAnotherUnrecognizedProperty()
    {
        using var context = CreateContext();
        var maxLength = GetMaxLength<TestEntityWithNoMatch>(context, nameof(TestEntityWithNoMatch.AnotherField));
        maxLength.Should().BeNull();
    }

    #endregion

    #region Case Insensitivity Tests

    private class TestEntityWithCaseVariations
    {
        public Guid Id { get; set; }
        public string? NAME { get; set; }
        public string? email { get; set; }
        public string? DESCRIPTION { get; set; }
    }

    private class TestDbContextWithCaseVariations : DbContext
    {
        public DbSet<TestEntityWithCaseVariations> TestEntities => Set<TestEntityWithCaseVariations>();

        public TestDbContextWithCaseVariations(DbContextOptions<TestDbContextWithCaseVariations> options) : base(options) { }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Conventions.Add(_ => new StringLengthByNameConvention());
        }
    }

    [Fact]
    public void Convention_ShouldBeCaseInsensitive_ForUppercaseName()
    {
        var options = new DbContextOptionsBuilder<TestDbContextWithCaseVariations>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContextWithCaseVariations(options);
        var maxLength = context.Model.FindEntityType(typeof(TestEntityWithCaseVariations))
            ?.FindProperty(nameof(TestEntityWithCaseVariations.NAME))
            ?.GetMaxLength();

        maxLength.Should().Be(200);
    }

    [Fact]
    public void Convention_ShouldBeCaseInsensitive_ForLowercaseEmail()
    {
        var options = new DbContextOptionsBuilder<TestDbContextWithCaseVariations>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContextWithCaseVariations(options);
        var maxLength = context.Model.FindEntityType(typeof(TestEntityWithCaseVariations))
            ?.FindProperty(nameof(TestEntityWithCaseVariations.email))
            ?.GetMaxLength();

        maxLength.Should().Be(256);
    }

    #endregion
}
