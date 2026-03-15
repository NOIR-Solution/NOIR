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
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.Schemas.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldContainVnPaySchema()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Schemas.ShouldContainKey("vnpay");
        var vnpaySchema = result.Value.Schemas["vnpay"];
        vnpaySchema.Provider.ShouldBe("vnpay");
        vnpaySchema.DisplayName.ShouldBe("VNPay");
        vnpaySchema.Fields.ShouldNotBeEmpty();
        vnpaySchema.SupportsCod.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_ShouldContainMoMoSchema()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Schemas.ShouldContainKey("momo");
        var momoSchema = result.Value.Schemas["momo"];
        momoSchema.Provider.ShouldBe("momo");
        momoSchema.DisplayName.ShouldBe("MoMo");
        momoSchema.Fields.ShouldNotBeEmpty();
        momoSchema.SupportsCod.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_ShouldContainZaloPaySchema()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Schemas.ShouldContainKey("zalopay");
        var zaloPaySchema = result.Value.Schemas["zalopay"];
        zaloPaySchema.Provider.ShouldBe("zalopay");
        zaloPaySchema.DisplayName.ShouldBe("ZaloPay");
        zaloPaySchema.Fields.ShouldNotBeEmpty();
        zaloPaySchema.SupportsCod.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_ShouldContainSePaySchema()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Schemas.ShouldContainKey("sepay");
        var sepaySchema = result.Value.Schemas["sepay"];
        sepaySchema.Provider.ShouldBe("sepay");
        sepaySchema.DisplayName.ShouldBe("SePay");
        sepaySchema.Fields.ShouldNotBeEmpty();
        sepaySchema.SupportsCod.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_ShouldContainCodSchema()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Schemas.ShouldContainKey("cod");
        var codSchema = result.Value.Schemas["cod"];
        codSchema.Provider.ShouldBe("cod");
        codSchema.DisplayName.ShouldBe("Cash on Delivery");
        codSchema.SupportsCod.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_VnPaySchema_ShouldHaveRequiredFields()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var vnpaySchema = result.Value.Schemas["vnpay"];
        vnpaySchema.Fields.ShouldContain(f => f.Key == "TmnCode" && f.Required);
        vnpaySchema.Fields.ShouldContain(f => f.Key == "HashSecret" && f.Required);
    }

    [Fact]
    public async Task Handle_VnPaySchema_ShouldHaveEnvironmentDefaults()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var vnpaySchema = result.Value.Schemas["vnpay"];
        vnpaySchema.Environments.ShouldNotBeNull();
        vnpaySchema.Environments.Sandbox.ShouldNotBeEmpty();
        vnpaySchema.Environments.Production.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Handle_SePaySchema_ShouldHaveBankOptions()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var sepaySchema = result.Value.Schemas["sepay"];
        var bankField = sepaySchema.Fields.FirstOrDefault(f => f.Key == "BankCode");
        bankField.ShouldNotBeNull();
        bankField!.Type.ShouldBe("select");
        bankField.Options.ShouldNotBeEmpty();
        bankField.Options.ShouldContain(o => o.Value == "MB");
        bankField.Options.ShouldContain(o => o.Value == "VCB");
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
        result1.IsSuccess.ShouldBe(true);
        result2.IsSuccess.ShouldBe(true);
        result1.Value.Schemas.Count.ShouldBe(result2.Value.Schemas.Count);
    }

    [Fact]
    public async Task Handle_AllSchemas_ShouldHaveDocumentationUrl()
    {
        // Arrange
        var query = new GetGatewaySchemasQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var schemasWithDocs = result.Value.Schemas.Values
            .Where(s => !string.IsNullOrEmpty(s.DocumentationUrl))
            .ToList();
        // VNPay, MoMo, ZaloPay, SePay should have docs - COD doesn't
        schemasWithDocs.Count().ShouldBeGreaterThanOrEqualTo(4);
    }

    #endregion
}
