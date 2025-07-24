using CreditTransfer.Core.Domain.Constants;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace CreditTransfer.Core.Domain.Tests;

/// <summary>
/// Unit tests for ErrorCodes constants class
/// Tests all error code values, uniqueness, documentation consistency, and mapping validation
/// </summary>
public class ErrorCodesTests
{
    #region Error Code Value Tests

    [Fact]
    public void ErrorCodes_Should_Have_Correct_Success_Value()
    {
        // Act & Assert
        ErrorCodes.Success.Should().Be(0);
    }

    [Fact]
    public void ErrorCodes_Should_Have_Correct_Exception_Mapping_Values()
    {
        // Act & Assert - Testing original exception mapping error codes
        ErrorCodes.UnknownSubscriber.Should().Be(2);
        ErrorCodes.SourceDestinationSame.Should().Be(3);
        ErrorCodes.InvalidPin.Should().Be(4);
        ErrorCodes.TransferAmountBelowMin.Should().Be(5);
        ErrorCodes.TransferAmountAboveMax.Should().Be(7);
        ErrorCodes.MiscellaneousError.Should().Be(14);
        ErrorCodes.InvalidSourcePhone.Should().Be(20);
        ErrorCodes.InvalidDestinationPhone.Should().Be(21);
        ErrorCodes.InvalidPIN.Should().Be(22);
        ErrorCodes.InsufficientBalance.Should().Be(23);
        ErrorCodes.SubscriptionNotFound.Should().Be(24);
        ErrorCodes.ConcurrentUpdateDetected.Should().Be(25);
        ErrorCodes.SourcePhoneNotFound.Should().Be(26);
        ErrorCodes.DestinationPhoneNotFound.Should().Be(27);
        ErrorCodes.UserNotAllowed.Should().Be(28);
        ErrorCodes.ConfigurationError.Should().Be(29);
        ErrorCodes.PropertyNotFound.Should().Be(30);
        ErrorCodes.ExpiredReservationCode.Should().Be(31);
        ErrorCodes.BadRequest.Should().Be(32);
        ErrorCodes.NotAllowedToTransfer.Should().Be(33);
        ErrorCodes.ExceedsMaxPerDay.Should().Be(34);
        ErrorCodes.RemainingBalance.Should().Be(35);
        ErrorCodes.AmountNotMultipleOfFive.Should().Be(36);
        ErrorCodes.SmsError.Should().Be(37);
        ErrorCodes.ReserveAmountError.Should().Be(38);
        ErrorCodes.CreditFailure.Should().Be(39);
        ErrorCodes.RemainingBalanceHalf.Should().Be(40);
        ErrorCodes.CreditTransferServiceIsBlocked.Should().Be(41);
        ErrorCodes.OCSTimeout.Should().Be(42);
        ErrorCodes.ExceedsMaxCapPerDay.Should().Be(43);
        ErrorCodes.ServiceUnavailable.Should().Be(999);
    }

    [Theory]
    [InlineData(0, "Success")]
    [InlineData(2, "UnknownSubscriber")]
    [InlineData(3, "SourceDestinationSame")]
    [InlineData(4, "InvalidPin")]
    [InlineData(5, "TransferAmountBelowMin")]
    [InlineData(7, "TransferAmountAboveMax")]
    [InlineData(14, "MiscellaneousError")]
    [InlineData(20, "InvalidSourcePhone")]
    [InlineData(21, "InvalidDestinationPhone")]
    [InlineData(22, "InvalidPIN")]
    [InlineData(23, "InsufficientBalance")]
    [InlineData(24, "SubscriptionNotFound")]
    [InlineData(25, "ConcurrentUpdateDetected")]
    [InlineData(26, "SourcePhoneNotFound")]
    [InlineData(27, "DestinationPhoneNotFound")]
    [InlineData(28, "UserNotAllowed")]
    [InlineData(29, "ConfigurationError")]
    [InlineData(30, "PropertyNotFound")]
    [InlineData(31, "ExpiredReservationCode")]
    [InlineData(32, "BadRequest")]
    [InlineData(33, "NotAllowedToTransfer")]
    [InlineData(34, "ExceedsMaxPerDay")]
    [InlineData(35, "RemainingBalance")]
    [InlineData(36, "AmountNotMultipleOfFive")]
    [InlineData(37, "SmsError")]
    [InlineData(38, "ReserveAmountError")]
    [InlineData(39, "CreditFailure")]
    [InlineData(40, "RemainingBalanceHalf")]
    [InlineData(41, "CreditTransferServiceIsBlocked")]
    [InlineData(42, "OCSTimeout")]
    [InlineData(43, "ExceedsMaxCapPerDay")]
    [InlineData(999, "ServiceUnavailable")]
    public void ErrorCodes_Should_Have_Correct_Value_For_Field(int expectedValue, string fieldName)
    {
        // Arrange
        var field = typeof(ErrorCodes).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        
        // Act & Assert
        field.Should().NotBeNull($"Field {fieldName} should exist");
        var actualValue = (int)field!.GetValue(null)!;
        actualValue.Should().Be(expectedValue, $"Field {fieldName} should have value {expectedValue}");
    }

    #endregion

    #region Error Code Uniqueness Tests

    [Fact]
    public void ErrorCodes_Should_Have_Unique_Values()
    {
        // Arrange
        var errorCodeFields = typeof(ErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        
        // Act
        var errorCodeValues = errorCodeFields.Select(f => (int)f.GetValue(null)!).ToList();
        
        // Assert
        errorCodeValues.Should().OnlyHaveUniqueItems("All error codes should have unique values");
    }

    [Fact]
    public void ErrorCodes_Should_Have_All_Expected_Fields()
    {
        // Arrange
        var expectedFields = new[]
        {
            "Success", "UnknownSubscriber", "SourceDestinationSame", "InvalidPin", "TransferAmountBelowMin",
            "TransferAmountAboveMax", "MiscellaneousError", "InvalidSourcePhone", "InvalidDestinationPhone",
            "InvalidPIN", "InsufficientBalance", "SubscriptionNotFound", "ConcurrentUpdateDetected",
            "SourcePhoneNotFound", "DestinationPhoneNotFound", "UserNotAllowed", "ConfigurationError",
            "PropertyNotFound", "ExpiredReservationCode", "BadRequest", "NotAllowedToTransfer",
            "ExceedsMaxPerDay", "RemainingBalance", "AmountNotMultipleOfFive", "SmsError",
            "ReserveAmountError", "CreditFailure", "RemainingBalanceHalf", "CreditTransferServiceIsBlocked",
            "OCSTimeout", "ExceedsMaxCapPerDay", "ServiceUnavailable"
        };
        
        // Act
        var actualFields = typeof(ErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(f => f.Name).ToArray();
        
        // Assert
        actualFields.Should().HaveCount(32, "Should have 32 error code fields");
        actualFields.Should().BeEquivalentTo(expectedFields);
    }

    [Fact]
    public void ErrorCodes_Should_Have_No_Duplicate_Values()
    {
        // Arrange
        var errorCodeFields = typeof(ErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var errorCodeValues = new List<int>();
        
        // Act
        foreach (var field in errorCodeFields)
        {
            var value = (int)field.GetValue(null)!;
            errorCodeValues.Add(value);
        }
        
        // Assert
        var duplicates = errorCodeValues.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
        duplicates.Should().BeEmpty($"Found duplicate error code values: {string.Join(", ", duplicates)}");
    }

    #endregion

    #region Error Code Range Tests

    [Fact]
    public void ErrorCodes_Should_Have_Success_As_Zero()
    {
        // Act & Assert
        ErrorCodes.Success.Should().Be(0, "Success should always be 0");
    }

    [Fact]
    public void ErrorCodes_Should_Have_Positive_Error_Values()
    {
        // Arrange
        var errorCodeFields = typeof(ErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        
        // Act & Assert
        foreach (var field in errorCodeFields)
        {
            var value = (int)field.GetValue(null)!;
            if (field.Name != "Success")
            {
                value.Should().BePositive($"Error code {field.Name} should be positive");
            }
        }
    }

    [Fact]
    public void ErrorCodes_Should_Have_Valid_Range_Values()
    {
        // Arrange
        var errorCodeFields = typeof(ErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        
        // Act & Assert
        foreach (var field in errorCodeFields)
        {
            var value = (int)field.GetValue(null)!;
            value.Should().BeInRange(0, 999, $"Error code {field.Name} should be in valid range 0-999");
        }
    }

    #endregion

    #region Error Code Category Tests

    [Fact]
    public void ErrorCodes_Should_Have_Original_Exception_Mapping_Codes()
    {
        // Arrange - These are the original exception codes that must be preserved
        var originalExceptionCodes = new Dictionary<int, string>
        {
            { 2, "UnknownSubscriber" },
            { 3, "SourceDestinationSame" },
            { 4, "InvalidPin" },
            { 5, "TransferAmountBelowMin" },
            { 7, "TransferAmountAboveMax" },
            { 14, "MiscellaneousError" },
            { 20, "InvalidSourcePhone" },
            { 21, "InvalidDestinationPhone" },
            { 22, "InvalidPIN" },
            { 23, "InsufficientBalance" },
            { 24, "SubscriptionNotFound" },
            { 25, "ConcurrentUpdateDetected" },
            { 26, "SourcePhoneNotFound" },
            { 27, "DestinationPhoneNotFound" },
            { 28, "UserNotAllowed" },
            { 29, "ConfigurationError" },
            { 30, "PropertyNotFound" },
            { 31, "ExpiredReservationCode" },
            { 33, "NotAllowedToTransfer" },
            { 34, "ExceedsMaxPerDay" },
            { 35, "RemainingBalance" },
            { 36, "AmountNotMultipleOfFive" },
            { 37, "SmsError" },
            { 38, "ReserveAmountError" },
            { 39, "CreditFailure" },
            { 40, "RemainingBalanceHalf" },
            { 41, "CreditTransferServiceIsBlocked" },
            { 42, "OCSTimeout" },
            { 43, "ExceedsMaxCapPerDay" }
        };
        
        // Act & Assert
        foreach (var (expectedCode, fieldName) in originalExceptionCodes)
        {
            var field = typeof(ErrorCodes).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            field.Should().NotBeNull($"Original exception code field {fieldName} should exist");
            
            var actualValue = (int)field!.GetValue(null)!;
            actualValue.Should().Be(expectedCode, $"Original exception code {fieldName} should have value {expectedCode}");
        }
    }

    [Fact]
    public void ErrorCodes_Should_Have_New_System_Codes()
    {
        // Arrange - New codes added for the migrated system
        var newSystemCodes = new Dictionary<int, string>
        {
            { 32, "BadRequest" },
            { 999, "ServiceUnavailable" }
        };
        
        // Act & Assert
        foreach (var (expectedCode, fieldName) in newSystemCodes)
        {
            var field = typeof(ErrorCodes).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            field.Should().NotBeNull($"New system code field {fieldName} should exist");
            
            var actualValue = (int)field!.GetValue(null)!;
            actualValue.Should().Be(expectedCode, $"New system code {fieldName} should have value {expectedCode}");
        }
    }

    #endregion

    #region Error Code Accessibility Tests

    [Fact]
    public void ErrorCodes_Fields_Should_Be_Public_Static_Const()
    {
        // Arrange
        var errorCodeFields = typeof(ErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        
        // Act & Assert
        foreach (var field in errorCodeFields)
        {
            field.IsPublic.Should().BeTrue($"Field {field.Name} should be public");
            field.IsStatic.Should().BeTrue($"Field {field.Name} should be static");
            field.IsLiteral.Should().BeTrue($"Field {field.Name} should be const");
            field.FieldType.Should().Be(typeof(int), $"Field {field.Name} should be of type int");
        }
    }

    [Fact]
    public void ErrorCodes_Class_Should_Be_Public_Static()
    {
        // Arrange
        var errorCodesType = typeof(ErrorCodes);
        
        // Act & Assert
        errorCodesType.IsPublic.Should().BeTrue("ErrorCodes class should be public");
        errorCodesType.IsAbstract.Should().BeTrue("ErrorCodes class should be static (abstract in IL)");
        errorCodesType.IsSealed.Should().BeTrue("ErrorCodes class should be static (sealed in IL)");
    }

    [Fact]
    public void ErrorCodes_Should_Have_No_Instance_Members()
    {
        // Arrange
        var errorCodesType = typeof(ErrorCodes);
        
        // Act
        var instanceMembers = errorCodesType.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        
        // Assert
        instanceMembers.Should().BeEmpty("ErrorCodes should have no instance members");
    }

    #endregion

    #region Error Code Documentation Tests

    [Fact]
    public void ErrorCodes_Should_Have_Proper_Namespace()
    {
        // Act & Assert
        typeof(ErrorCodes).Namespace.Should().Be("CreditTransfer.Core.Domain.Constants");
    }

    [Fact]
    public void ErrorCodes_Should_Have_Consistent_Naming_Convention()
    {
        // Arrange
        var errorCodeFields = typeof(ErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        
        // Act & Assert
        foreach (var field in errorCodeFields)
        {
            // Field names should be PascalCase and not contain numbers
            field.Name.Should().MatchRegex("^[A-Z][a-zA-Z]*$", $"Field {field.Name} should follow PascalCase convention");
            field.Name.Should().NotContain("_", $"Field {field.Name} should not contain underscores");
        }
    }

    [Fact]
    public void ErrorCodes_Should_Have_Meaningful_Names()
    {
        // Arrange
        var errorCodeFields = typeof(ErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        
        // Act & Assert
        foreach (var field in errorCodeFields)
        {
            // Field names should be descriptive and not too short
            field.Name.Length.Should().BeGreaterThanOrEqualTo(3, $"Field {field.Name} should have a meaningful name");
            field.Name.Should().NotBe("Error", $"Field {field.Name} should be more specific than generic 'Error'");
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ErrorCodes_Should_Cover_All_Critical_Scenarios()
    {
        // Arrange - Critical business scenarios that must have error codes
        var criticalScenarios = new[]
        {
            "Success", "UnknownSubscriber", "InvalidPin", "InsufficientBalance",
            "SourcePhoneNotFound", "DestinationPhoneNotFound", "NotAllowedToTransfer",
            "ExceedsMaxPerDay", "ConfigurationError", "ServiceUnavailable"
        };
        
        // Act
        var errorCodeFields = typeof(ErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var fieldNames = errorCodeFields.Select(f => f.Name).ToArray();
        
        // Assert
        foreach (var scenario in criticalScenarios)
        {
            fieldNames.Should().Contain(scenario, $"Critical scenario {scenario} should have an error code");
        }
    }

    [Fact]
    public void ErrorCodes_Should_Have_Reasonable_Distribution()
    {
        // Arrange
        var errorCodeFields = typeof(ErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var errorCodeValues = errorCodeFields.Select(f => (int)f.GetValue(null)!).ToList();
        
        // Act
        var lowRangeCount = errorCodeValues.Count(v => v >= 0 && v <= 50);
        var highRangeCount = errorCodeValues.Count(v => v > 50);
        
        // Assert
        lowRangeCount.Should().BeGreaterThan(0, "Should have error codes in low range (0-50)");
        errorCodeValues.Should().Contain(0, "Should have success code");
        errorCodeValues.Max().Should().BeLessThanOrEqualTo(999, "Should not exceed maximum error code range");
    }

    #endregion
} 