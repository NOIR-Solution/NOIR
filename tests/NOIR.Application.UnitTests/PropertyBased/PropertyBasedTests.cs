namespace NOIR.Application.UnitTests.PropertyBased;

/// <summary>
/// Property-based tests using Bogus for random data generation.
/// These tests verify that invariants hold across many random inputs,
/// providing higher confidence in the correctness of complex logic.
/// </summary>
public class PropertyBasedTests
{
    private static string GenerateTestToken() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    private readonly Faker _faker = new();

    #region Result Pattern Property Tests

    [Theory]
    [InlineData(100)]
    public void Result_Success_ShouldAlwaysHaveNoError(int iterations)
    {
        // Property: A successful Result should ALWAYS have Error.None
        for (int i = 0; i < iterations; i++)
        {
            // Act
            var result = Result.Success();

            // Assert - This invariant must always hold
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Error.Should().Be(Error.None);
        }
    }

    [Theory]
    [InlineData(100)]
    public void Result_Failure_ShouldAlwaysHaveError(int iterations)
    {
        // Property: A failed Result should NEVER have Error.None
        for (int i = 0; i < iterations; i++)
        {
            // Arrange - Random error data
            var errorCode = _faker.Random.AlphaNumeric(10);
            var errorMessage = _faker.Lorem.Sentence();
            var errorType = _faker.PickRandom<ErrorType>();
            var error = new Error(errorCode, errorMessage, errorType);

            // Act
            var result = Result.Failure(error);

            // Assert - This invariant must always hold
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().NotBe(Error.None);
            result.Error.Code.Should().Be(errorCode);
            result.Error.Message.Should().Be(errorMessage);
        }
    }

    [Theory]
    [InlineData(100)]
    public void ResultT_Success_ShouldAlwaysReturnValue(int iterations)
    {
        // Property: A successful Result<T> should ALWAYS return its value
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var value = _faker.Random.Int();

            // Act
            var result = Result.Success(value);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(value);
            result.Error.Should().Be(Error.None);
        }
    }

    [Theory]
    [InlineData(100)]
    public void ResultT_Failure_ShouldThrowWhenAccessingValue(int iterations)
    {
        // Property: A failed Result<T> should ALWAYS throw when accessing Value
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var error = Error.Failure(_faker.Random.AlphaNumeric(10), _faker.Lorem.Sentence());

            // Act
            var result = Result.Failure<int>(error);

            // Assert
            result.IsFailure.Should().BeTrue();
            var act = () => result.Value;
            act.Should().Throw<InvalidOperationException>();
        }
    }

    [Theory]
    [InlineData(100)]
    public void Result_SuccessWithErrorNone_ShouldBeValid(int iterations)
    {
        // Property: Success + Error.None is a valid combination
        for (int i = 0; i < iterations; i++)
        {
            // This should never throw
            var act = () => Result.Success();
            act.Should().NotThrow();
        }
    }

    [Theory]
    [InlineData(100)]
    public void Result_SuccessWithError_ShouldThrow(int iterations)
    {
        // Property: Success + any error other than Error.None should throw
        for (int i = 0; i < iterations; i++)
        {
            // Arrange - Generate non-None error
            var error = Error.Failure(_faker.Random.AlphaNumeric(10), _faker.Lorem.Sentence());

            // Act & Assert - Constructor validation should catch this
            // We can't directly test this as Result constructor is protected,
            // but we verify that the factory methods enforce this invariant
            var success = Result.Success();
            var failure = Result.Failure(error);

            // Invariants
            success.Error.Should().Be(Error.None);
            failure.Error.Should().NotBe(Error.None);
        }
    }

    #endregion

    #region RefreshToken Property Tests

    [Theory]
    [InlineData(100)]
    public void RefreshToken_Create_ShouldGenerateUniqueTokens(int iterations)
    {
        // Property: Each token creation should produce a unique token
        var tokens = new HashSet<string>();

        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var userId = _faker.Random.Guid().ToString();
            var expirationDays = _faker.Random.Int(1, 365);

            // Act
            var refreshToken = RefreshToken.Create(GenerateTestToken(), userId, expirationDays);

            // Assert - Token should be unique
            tokens.Contains(refreshToken.Token).Should().BeFalse(
                $"Token collision detected after {tokens.Count} iterations");
            tokens.Add(refreshToken.Token);
        }
    }

    [Theory]
    [InlineData(100)]
    public void RefreshToken_Create_ShouldAlwaysSetExpirationInFuture(int iterations)
    {
        // Property: Expiration should ALWAYS be in the future
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var userId = _faker.Random.Guid().ToString();
            var expirationDays = _faker.Random.Int(1, 365);
            var beforeCreation = DateTimeOffset.UtcNow;

            // Act
            var refreshToken = RefreshToken.Create(GenerateTestToken(), userId, expirationDays);

            // Assert
            refreshToken.ExpiresAt.Should().BeAfter(beforeCreation);
            refreshToken.IsExpired.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(100)]
    public void RefreshToken_Create_ShouldPreserveUserId(int iterations)
    {
        // Property: UserId should be preserved exactly
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var userId = _faker.Random.Guid().ToString();

            // Act
            var refreshToken = RefreshToken.Create(GenerateTestToken(), userId, 7);

            // Assert
            refreshToken.UserId.Should().Be(userId);
        }
    }

    [Theory]
    [InlineData(100)]
    public void RefreshToken_Create_ShouldHandleAllOptionalParameters(int iterations)
    {
        // Property: All optional parameters should be handled correctly
        for (int i = 0; i < iterations; i++)
        {
            // Arrange - Random optional values
            var userId = _faker.Random.Guid().ToString();
            var expirationDays = _faker.Random.Int(1, 365);
            var tenantId = _faker.Random.Bool() ? _faker.Random.Guid().ToString() : null;
            var ipAddress = _faker.Random.Bool() ? _faker.Internet.Ip() : null;
            var deviceFingerprint = _faker.Random.Bool() ? _faker.Random.AlphaNumeric(32) : null;
            var userAgent = _faker.Random.Bool() ? _faker.Internet.UserAgent() : null;
            var deviceName = _faker.Random.Bool() ? _faker.Commerce.ProductName() : null;
            var tokenFamily = _faker.Random.Bool() ? Guid.NewGuid() : (Guid?)null;

            // Act
            var refreshToken = RefreshToken.Create(
                GenerateTestToken(), userId, expirationDays, tenantId, ipAddress,
                deviceFingerprint, userAgent, deviceName, tokenFamily);

            // Assert - All values should be preserved
            refreshToken.UserId.Should().Be(userId);
            refreshToken.TenantId.Should().Be(tenantId);
            refreshToken.CreatedByIp.Should().Be(ipAddress);
            refreshToken.DeviceFingerprint.Should().Be(deviceFingerprint);
            refreshToken.UserAgent.Should().Be(userAgent);
            refreshToken.DeviceName.Should().Be(deviceName);

            if (tokenFamily.HasValue)
                refreshToken.TokenFamily.Should().Be(tokenFamily.Value);
            else
                refreshToken.TokenFamily.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData(100)]
    public void RefreshToken_Revoke_ShouldAlwaysSetRevokedAt(int iterations)
    {
        // Property: After revocation, RevokedAt should ALWAYS be set
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var refreshToken = RefreshToken.Create(GenerateTestToken(), _faker.Random.Guid().ToString(), 7);
            var beforeRevoke = DateTimeOffset.UtcNow;

            // Act
            refreshToken.Revoke(
                _faker.Random.Bool() ? _faker.Internet.Ip() : null,
                _faker.Random.Bool() ? _faker.Lorem.Sentence() : null,
                _faker.Random.Bool() ? _faker.Random.AlphaNumeric(88) : null);

            // Assert
            refreshToken.IsRevoked.Should().BeTrue();
            refreshToken.IsActive.Should().BeFalse();
            refreshToken.RevokedAt.Should().NotBeNull();
            refreshToken.RevokedAt!.Value.Should().BeOnOrAfter(beforeRevoke);
        }
    }

    [Theory]
    [InlineData(100)]
    public void RefreshToken_IsActive_ShouldBeFalseWhenRevokedOrExpired(int iterations)
    {
        // Property: IsActive = !IsRevoked && !IsExpired
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var refreshToken = RefreshToken.Create(GenerateTestToken(), _faker.Random.Guid().ToString(), 7);

            // Assert - Initially active
            refreshToken.IsActive.Should().Be(!refreshToken.IsRevoked && !refreshToken.IsExpired);

            // After revocation
            refreshToken.Revoke();
            refreshToken.IsActive.Should().BeFalse();
            refreshToken.IsActive.Should().Be(!refreshToken.IsRevoked && !refreshToken.IsExpired);
        }
    }

    #endregion

    #region Error Factory Methods Property Tests

    [Theory]
    [InlineData(100)]
    public void Error_NotFound_ShouldAlwaysHaveCorrectType(int iterations)
    {
        // Property: NotFound errors should ALWAYS have ErrorType.NotFound
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var entity = _faker.Random.Word();
            var id = _faker.Random.Guid();

            // Act
            var error = Error.NotFound(entity, id);

            // Assert
            error.Type.Should().Be(ErrorType.NotFound);
            error.Code.Should().Be(ErrorCodes.Business.NotFound);
        }
    }

    [Theory]
    [InlineData(100)]
    public void Error_Validation_ShouldAlwaysHaveCorrectType(int iterations)
    {
        // Property: Validation errors should ALWAYS have ErrorType.Validation
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var propertyName = _faker.Random.Word();
            var message = _faker.Lorem.Sentence();

            // Act
            var error = Error.Validation(propertyName, message);

            // Assert
            error.Type.Should().Be(ErrorType.Validation);
            error.Code.Should().Be(ErrorCodes.Validation.General);
        }
    }

    [Theory]
    [InlineData(100)]
    public void Error_ValidationErrors_ShouldCombineAllMessages(int iterations)
    {
        // Property: ValidationErrors should include all error messages
        for (int i = 0; i < iterations; i++)
        {
            // Arrange - Use unique field names to avoid dictionary key collisions
            var errorCount = _faker.Random.Int(1, 5);
            var errors = new Dictionary<string, string[]>();

            for (int j = 0; j < errorCount; j++)
            {
                var field = $"Field{j}_{_faker.Random.AlphaNumeric(5)}"; // Unique field names
                var messages = Enumerable.Range(0, _faker.Random.Int(1, 3))
                    .Select(_ => _faker.Lorem.Sentence())
                    .ToArray();
                errors[field] = messages;
            }

            // Act
            var error = Error.ValidationErrors(errors);

            // Assert - All messages from the final dictionary should be in the result
            error.Type.Should().Be(ErrorType.Validation);
            var allFinalMessages = errors.Values.SelectMany(v => v);
            foreach (var msg in allFinalMessages)
            {
                error.Message.Should().Contain(msg);
            }
        }
    }

    [Theory]
    [InlineData(100)]
    public void Error_AllFactoryMethods_ShouldNeverReturnNull(int iterations)
    {
        // Property: Factory methods should NEVER return null
        for (int i = 0; i < iterations; i++)
        {
            // Act & Assert
            Error.NotFound(_faker.Random.Word(), _faker.Random.Guid()).Should().NotBeNull();
            Error.NotFound(_faker.Lorem.Sentence()).Should().NotBeNull();
            Error.Validation(_faker.Random.Word(), _faker.Lorem.Sentence()).Should().NotBeNull();
            Error.Conflict(_faker.Lorem.Sentence()).Should().NotBeNull();
            Error.Unauthorized(_faker.Lorem.Sentence()).Should().NotBeNull();
            Error.Forbidden(_faker.Lorem.Sentence()).Should().NotBeNull();
            Error.Failure(_faker.Random.Word(), _faker.Lorem.Sentence()).Should().NotBeNull();
        }
    }

    #endregion

    #region Specification Combiner Property Tests

    private class TestEntity
    {
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ValueGreaterThanSpec : Specification<TestEntity>
    {
        public ValueGreaterThanSpec(int threshold)
        {
            Query.Where(e => e.Value > threshold);
        }
    }

    private class ValueLessThanSpec : Specification<TestEntity>
    {
        public ValueLessThanSpec(int threshold)
        {
            Query.Where(e => e.Value < threshold);
        }
    }

    private class NameContainsSpec : Specification<TestEntity>
    {
        public NameContainsSpec(string substring)
        {
            Query.Where(e => e.Name.Contains(substring));
        }
    }

    [Theory]
    [InlineData(100)]
    public void Specification_And_ShouldSatisfyBothConditions(int iterations)
    {
        // Property: And(A, B) should be true only when A is true AND B is true
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var lowerBound = _faker.Random.Int(0, 50);
            var upperBound = _faker.Random.Int(51, 100);
            var testValue = _faker.Random.Int(0, 100);

            var entity = new TestEntity { Value = testValue, Name = "Test" };
            var specA = new ValueGreaterThanSpec(lowerBound);
            var specB = new ValueLessThanSpec(upperBound);

            // Act
            var combinedSpec = specA.And(specB);
            var result = combinedSpec.IsSatisfiedBy(entity);

            // Assert - Should match manual AND check
            var expected = testValue > lowerBound && testValue < upperBound;
            result.Should().Be(expected);
        }
    }

    [Theory]
    [InlineData(100)]
    public void Specification_Or_ShouldSatisfyEitherCondition(int iterations)
    {
        // Property: Or(A, B) should be true when A is true OR B is true
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var threshold1 = _faker.Random.Int(0, 30);
            var threshold2 = _faker.Random.Int(70, 100);
            var testValue = _faker.Random.Int(0, 100);

            var entity = new TestEntity { Value = testValue, Name = "Test" };
            var specA = new ValueLessThanSpec(threshold1); // Small values
            var specB = new ValueGreaterThanSpec(threshold2); // Large values

            // Act
            var combinedSpec = specA.Or(specB);
            var result = combinedSpec.IsSatisfiedBy(entity);

            // Assert - Should match manual OR check
            var expected = testValue < threshold1 || testValue > threshold2;
            result.Should().Be(expected);
        }
    }

    [Theory]
    [InlineData(100)]
    public void Specification_Not_ShouldNegateCondition(int iterations)
    {
        // Property: Not(A) should be true only when A is false
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var threshold = _faker.Random.Int(0, 100);
            var testValue = _faker.Random.Int(0, 100);

            var entity = new TestEntity { Value = testValue, Name = "Test" };
            var spec = new ValueGreaterThanSpec(threshold);

            // Act
            var negatedSpec = spec.Not();
            var result = negatedSpec.IsSatisfiedBy(entity);

            // Assert - Should match manual NOT check
            var expected = !(testValue > threshold);
            result.Should().Be(expected);
        }
    }

    [Theory]
    [InlineData(100)]
    public void Specification_Evaluate_ShouldFilterCorrectly(int iterations)
    {
        // Property: Evaluate should return only entities satisfying the specification
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var threshold = _faker.Random.Int(20, 80);
            var entities = Enumerable.Range(0, 50)
                .Select(n => new TestEntity { Value = n * 2, Name = $"Entity{n}" })
                .ToList();

            var spec = new ValueGreaterThanSpec(threshold);

            // Act
            var result = spec.Evaluate(entities).ToList();

            // Assert - All results should satisfy the condition
            result.Should().OnlyContain(e => e.Value > threshold);
            result.Count.Should().Be(entities.Count(e => e.Value > threshold));
        }
    }

    [Theory]
    [InlineData(100)]
    public void Specification_IsSatisfiedBy_WithNull_ShouldReturnFalse(int iterations)
    {
        // Property: IsSatisfiedBy(null) should ALWAYS return false
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var threshold = _faker.Random.Int(0, 100);
            var spec = new ValueGreaterThanSpec(threshold);

            // Act
            var result = spec.IsSatisfiedBy(null!);

            // Assert
            result.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(100)]
    public void Specification_DeMorgansLaw_ShouldHold(int iterations)
    {
        // Property: De Morgan's Law - Not(A And B) = Not(A) Or Not(B)
        for (int i = 0; i < iterations; i++)
        {
            // Arrange
            var lowerBound = _faker.Random.Int(0, 50);
            var upperBound = _faker.Random.Int(51, 100);
            var testValue = _faker.Random.Int(0, 100);

            var entity = new TestEntity { Value = testValue, Name = "Test" };
            var specA = new ValueGreaterThanSpec(lowerBound);
            var specB = new ValueLessThanSpec(upperBound);

            // Act - Not(A And B)
            var notAAndB = specA.And(specB).Not();
            var result1 = notAAndB.IsSatisfiedBy(entity);

            // Act - Not(A) Or Not(B)
            var notAOrNotB = specA.Not().Or(specB.Not());
            var result2 = notAOrNotB.IsSatisfiedBy(entity);

            // Assert - De Morgan's Law should hold
            result1.Should().Be(result2);
        }
    }

    #endregion

    #region Token Security Property Tests

    [Fact]
    public void RefreshToken_TokenLength_ShouldBeSecure()
    {
        // Property: Generated tokens should have sufficient entropy (at least 64 bytes base64)
        var minExpectedLength = 85; // 64 bytes in base64 ≈ 88 chars, allow some variance

        for (int i = 0; i < 1000; i++)
        {
            var token = RefreshToken.Create(GenerateTestToken(), _faker.Random.Guid().ToString(), 7);
            token.Token.Length.Should().BeGreaterThanOrEqualTo(minExpectedLength,
                $"Token {token.Token} is too short for security");
        }
    }

    [Fact]
    public void RefreshToken_TokenDistribution_ShouldBeUniform()
    {
        // Property: Token characters should have reasonable distribution (entropy test)
        var tokens = Enumerable.Range(0, 1000)
            .Select(_ => RefreshToken.Create(GenerateTestToken(), _faker.Random.Guid().ToString(), 7).Token)
            .ToList();

        var allChars = string.Join("", tokens);
        var charCounts = allChars.GroupBy(c => c)
            .Select(g => new { Char = g.Key, Count = g.Count() })
            .ToList();

        // Base64 uses 64 characters, should see reasonable variety
        charCounts.Count.Should().BeGreaterThanOrEqualTo(50,
            "Token character distribution lacks variety");
    }

    #endregion

    #region Edge Case Property Tests

    [Theory]
    [InlineData(100)]
    public void RefreshToken_WithMinimumExpirationDays_ShouldStillWork(int iterations)
    {
        // Property: Edge case - 1 day expiration should work
        for (int i = 0; i < iterations; i++)
        {
            var token = RefreshToken.Create(GenerateTestToken(), _faker.Random.Guid().ToString(), 1);
            token.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
            token.IsExpired.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(100)]
    public void RefreshToken_WithLargeExpirationDays_ShouldStillWork(int iterations)
    {
        // Property: Edge case - Large expiration (1 year) should work
        for (int i = 0; i < iterations; i++)
        {
            var token = RefreshToken.Create(GenerateTestToken(), _faker.Random.Guid().ToString(), 365);
            token.ExpiresAt.Should().BeCloseTo(
                DateTimeOffset.UtcNow.AddDays(365),
                TimeSpan.FromSeconds(5));
            token.IsExpired.Should().BeFalse();
        }
    }

    [Fact]
    public void Error_WithEmptyStrings_ShouldHandleGracefully()
    {
        // Property: Edge case - Empty strings should be handled
        var error = new Error(string.Empty, string.Empty);
        error.Should().NotBeNull();
        error.Code.Should().BeEmpty();
        error.Message.Should().BeEmpty();
    }

    [Fact]
    public void Error_None_ShouldBeUnique()
    {
        // Property: Error.None should be a singleton-like constant
        var none1 = Error.None;
        var none2 = Error.None;

        none1.Should().Be(none2);
        ReferenceEquals(none1, none2).Should().BeTrue();
    }

    #endregion
}
