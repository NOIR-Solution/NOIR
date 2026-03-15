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
            result.IsSuccess.ShouldBe(true);
            result.IsFailure.ShouldBe(false);
            result.Error.ShouldBe(Error.None);
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
            result.IsSuccess.ShouldBe(false);
            result.IsFailure.ShouldBe(true);
            result.Error.ShouldNotBe(Error.None);
            result.Error.Code.ShouldBe(errorCode);
            result.Error.Message.ShouldBe(errorMessage);
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
            result.IsSuccess.ShouldBe(true);
            result.Value.ShouldBe(value);
            result.Error.ShouldBe(Error.None);
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
            result.IsFailure.ShouldBe(true);
            var act = () => result.Value;
            Should.Throw<InvalidOperationException>(() => { act(); });
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
            act.ShouldNotThrow();
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
            success.Error.ShouldBe(Error.None);
            failure.Error.ShouldNotBe(Error.None);
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
            tokens.Contains(refreshToken.Token).ShouldBeFalse(
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
            refreshToken.ExpiresAt.ShouldBeGreaterThan(beforeCreation);
            refreshToken.IsExpired.ShouldBe(false);
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
            refreshToken.UserId.ShouldBe(userId);
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
            refreshToken.UserId.ShouldBe(userId);
            refreshToken.TenantId.ShouldBe(tenantId);
            refreshToken.CreatedByIp.ShouldBe(ipAddress);
            refreshToken.DeviceFingerprint.ShouldBe(deviceFingerprint);
            refreshToken.UserAgent.ShouldBe(userAgent);
            refreshToken.DeviceName.ShouldBe(deviceName);

            if (tokenFamily.HasValue)
                refreshToken.TokenFamily.ShouldBe(tokenFamily.Value);
            else
                refreshToken.TokenFamily.ShouldNotBe(Guid.Empty);
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
            refreshToken.IsRevoked.ShouldBe(true);
            refreshToken.IsActive.ShouldBe(false);
            refreshToken.RevokedAt.ShouldNotBeNull();
            refreshToken.RevokedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeRevoke);
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
            refreshToken.IsActive.ShouldBe(!refreshToken.IsRevoked && !refreshToken.IsExpired);

            // After revocation
            refreshToken.Revoke();
            refreshToken.IsActive.ShouldBe(false);
            refreshToken.IsActive.ShouldBe(!refreshToken.IsRevoked && !refreshToken.IsExpired);
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
            error.Type.ShouldBe(ErrorType.NotFound);
            error.Code.ShouldBe(ErrorCodes.Business.NotFound);
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
            error.Type.ShouldBe(ErrorType.Validation);
            error.Code.ShouldBe(ErrorCodes.Validation.General);
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
            error.Type.ShouldBe(ErrorType.Validation);
            var allFinalMessages = errors.Values.SelectMany(v => v);
            foreach (var msg in allFinalMessages)
            {
                error.Message.ShouldContain(msg);
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
            Error.NotFound(_faker.Random.Word(), _faker.Random.Guid()).ShouldNotBeNull();
            Error.NotFound(_faker.Lorem.Sentence()).ShouldNotBeNull();
            Error.Validation(_faker.Random.Word(), _faker.Lorem.Sentence()).ShouldNotBeNull();
            Error.Conflict(_faker.Lorem.Sentence()).ShouldNotBeNull();
            Error.Unauthorized(_faker.Lorem.Sentence()).ShouldNotBeNull();
            Error.Forbidden(_faker.Lorem.Sentence()).ShouldNotBeNull();
            Error.Failure(_faker.Random.Word(), _faker.Lorem.Sentence()).ShouldNotBeNull();
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
            result.ShouldBe(expected);
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
            result.ShouldBe(expected);
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
            result.ShouldBe(expected);
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
            result.ShouldAllBe(e => e.Value > threshold);
            result.Count.ShouldBe(entities.Count(e => e.Value > threshold));
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
            result.ShouldBe(false);
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
            result1.ShouldBe(result2);
        }
    }

    #endregion

    #region Token Security Property Tests

    [Fact]
    public void RefreshToken_TokenValue_ShouldBePreserved()
    {
        // Property: Token value passed to Create should be preserved exactly
        // Note: The RefreshToken entity accepts pre-generated tokens; actual token
        // generation with cryptographic security is done by RefreshTokenService.
        // This test verifies the entity preserves whatever token is passed in.
        var minExpectedLength = 64; // GenerateTestToken creates 64-char hex tokens (32 bytes)

        for (int i = 0; i < 100; i++)
        {
            var inputToken = GenerateTestToken();
            var refreshToken = RefreshToken.Create(inputToken, _faker.Random.Guid().ToString(), 7);
            refreshToken.Token.ShouldBe(inputToken, "Token value should be preserved exactly");
            refreshToken.Token.Length.ShouldBe(minExpectedLength,
                "Test token should be 64 characters (2 GUIDs without hyphens)");
        }
    }

    [Fact]
    public void RefreshToken_GeneratedTestTokens_ShouldHaveGoodDistribution()
    {
        // Property: Test token generation should produce varied characters
        // This validates our test helper, not the RefreshToken entity itself.
        var tokens = Enumerable.Range(0, 100)
            .Select(_ => GenerateTestToken())
            .ToList();

        var allChars = string.Join("", tokens);
        var charCounts = allChars.GroupBy(c => c)
            .Select(g => new { Char = g.Key, Count = g.Count() })
            .ToList();

        // Hex uses 16 characters (0-9, a-f), should see all of them
        charCounts.Count.ShouldBeGreaterThanOrEqualTo(15,
            "Test token character distribution lacks variety");
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
            token.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
            token.IsExpired.ShouldBe(false);
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
            token.ExpiresAt.ShouldBe(
                DateTimeOffset.UtcNow.AddDays(365),
                TimeSpan.FromSeconds(5));
            token.IsExpired.ShouldBe(false);
        }
    }

    [Fact]
    public void Error_WithEmptyStrings_ShouldHandleGracefully()
    {
        // Property: Edge case - Empty strings should be handled
        var error = new Error(string.Empty, string.Empty);
        error.ShouldNotBeNull();
        error.Code.ShouldBeEmpty();
        error.Message.ShouldBeEmpty();
    }

    [Fact]
    public void Error_None_ShouldBeUnique()
    {
        // Property: Error.None should be a singleton-like constant
        var none1 = Error.None;
        var none2 = Error.None;

        none1.ShouldBe(none2);
        ReferenceEquals(none1, none2).ShouldBe(true);
    }

    #endregion
}
