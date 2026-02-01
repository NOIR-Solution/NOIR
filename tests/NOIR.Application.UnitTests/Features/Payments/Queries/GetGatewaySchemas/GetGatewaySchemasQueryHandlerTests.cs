using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetGatewaySchemas;

namespace NOIR.Application.UnitTests.Features.Payments.Queries.GetGatewaySchemas;

/// <summary>
/// Unit tests for GetGatewaySchemasQueryHandler.
/// Tests retrieval of payment gateway credential schemas.
/// </summary>
public class GetGatewaySchemasQueryHandlerTests
{
    #region Test Setup

    private readonly GetGatewaySchemasQueryHandler _handler;

    public GetGatewaySchemasQueryHandlerTests()
    {
        _handler = new GetGatewaySchemasQueryHandler();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ShouldReturnGatewaySchemas()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Schemas.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldContainVnPaySchema()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Schemas.Should().ContainKey("vnpay");
        var vnpaySchema = result.Value.Schemas["vnpay"];
        vnpaySchema.Provider.Should().Be("vnpay");
        vnpaySchema.DisplayName.Should().Be("VNPay");
        vnpaySchema.Fields.Should().NotBeEmpty();
        vnpaySchema.SupportsCod.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldContainMoMoSchema()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Schemas.Should().ContainKey("momo");
        var momoSchema = result.Value.Schemas["momo"];
        momoSchema.Provider.Should().Be("momo");
        momoSchema.DisplayName.Should().Be("MoMo");
        momoSchema.Fields.Should().NotBeEmpty();
        momoSchema.SupportsCod.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldContainZaloPaySchema()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Schemas.Should().ContainKey("zalopay");
        var zaloPaySchema = result.Value.Schemas["zalopay"];
        zaloPaySchema.Provider.Should().Be("zalopay");
        zaloPaySchema.DisplayName.Should().Be("ZaloPay");
        zaloPaySchema.Fields.Should().NotBeEmpty();
        zaloPaySchema.SupportsCod.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldContainSePaySchema()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Schemas.Should().ContainKey("sepay");
        var sepaySchema = result.Value.Schemas["sepay"];
        sepaySchema.Provider.Should().Be("sepay");
        sepaySchema.DisplayName.Should().Be("SePay");
        sepaySchema.Fields.Should().NotBeEmpty();
        sepaySchema.SupportsCod.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldContainCodSchema()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Schemas.Should().ContainKey("cod");
        var codSchema = result.Value.Schemas["cod"];
        codSchema.Provider.Should().Be("cod");
        codSchema.DisplayName.Should().Be("Cash on Delivery");
        codSchema.SupportsCod.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_VnPaySchema_ShouldHaveRequiredFields()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var vnpaySchema = result.Value.Schemas["vnpay"];
        vnpaySchema.Fields.Should().Contain(f => f.Key == "TmnCode" && f.Required);
        vnpaySchema.Fields.Should().Contain(f => f.Key == "HashSecret" && f.Required);
    }

    [Fact]
    public async Task Handle_VnPaySchema_ShouldHaveEnvironmentDefaults()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var vnpaySchema = result.Value.Schemas["vnpay"];
        vnpaySchema.Environments.Should().NotBeNull();
        vnpaySchema.Environments.Sandbox.Should().NotBeEmpty();
        vnpaySchema.Environments.Production.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_SePaySchema_ShouldHaveBankOptions()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var sepaySchema = result.Value.Schemas["sepay"];
        var bankField = sepaySchema.Fields.FirstOrDefault(f => f.Key == "BankCode");
        bankField.Should().NotBeNull();
        bankField!.Type.Should().Be("select");
        bankField.Options.Should().NotBeNullOrEmpty();
        bankField.Options.Should().Contain(o => o.Value == "MB");
        bankField.Options.Should().Contain(o => o.Value == "VCB");
    }

    #endregion

    #region Static Schema Validation

    [Fact]
    public async Task Handle_MultipleCalls_ShouldReturnSameSchemas()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result1 = await _handler.Handle(query, CancellationToken.None);
        var result2 = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Schemas.Count.Should().Be(result2.Value.Schemas.Count);
    }

    [Fact]
    public async Task Handle_AllSchemas_ShouldHaveDocumentationUrl()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var schemasWithDocs = result.Value.Schemas.Values
            .Where(s => !string.IsNullOrEmpty(s.DocumentationUrl))
            .ToList();
        // VNPay, MoMo, ZaloPay, SePay should have docs - COD doesn't
        schemasWithDocs.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    #endregion
}
