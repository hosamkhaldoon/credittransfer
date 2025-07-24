using CreditTransfer.Core.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CreditTransfer.Core.Domain.Tests;

/// <summary>
/// Unit tests for SubscriptionType enum and related subscription enums
/// Tests enum values, conversions, parsing, and validation scenarios
/// </summary>
public class SubscriptionTypeTests
{
    #region SubscriptionType Tests

    [Fact]
    public void SubscriptionType_Should_Have_Correct_Values()
    {
        // Arrange & Act & Assert
        ((int)SubscriptionType.Customer).Should().Be(0);
        ((int)SubscriptionType.Pos).Should().Be(1);
        ((int)SubscriptionType.Distributor).Should().Be(2);
        ((int)SubscriptionType.DataAccount).Should().Be(3);
        ((int)SubscriptionType.HalafoniCustomer).Should().Be(4);
        ((int)SubscriptionType.Prepaid).Should().Be(5);
        ((int)SubscriptionType.VirginPrepaidCustomer).Should().Be(6);
        ((int)SubscriptionType.VirginPostpaidCustomer).Should().Be(7);
    }

    [Fact]
    public void SubscriptionType_Should_Have_All_Expected_Values()
    {
        // Arrange
        var expectedValues = new[]
        {
            SubscriptionType.Customer,
            SubscriptionType.Pos,
            SubscriptionType.Distributor,
            SubscriptionType.DataAccount,
            SubscriptionType.HalafoniCustomer,
            SubscriptionType.Prepaid,
            SubscriptionType.VirginPrepaidCustomer,
            SubscriptionType.VirginPostpaidCustomer
        };

        // Act
        var actualValues = Enum.GetValues<SubscriptionType>();

        // Assert
        actualValues.Should().HaveCount(8);
        actualValues.Should().BeEquivalentTo(expectedValues);
    }

    [Theory]
    [InlineData(0, SubscriptionType.Customer)]
    [InlineData(1, SubscriptionType.Pos)]
    [InlineData(2, SubscriptionType.Distributor)]
    [InlineData(3, SubscriptionType.DataAccount)]
    [InlineData(4, SubscriptionType.HalafoniCustomer)]
    [InlineData(5, SubscriptionType.Prepaid)]
    [InlineData(6, SubscriptionType.VirginPrepaidCustomer)]
    [InlineData(7, SubscriptionType.VirginPostpaidCustomer)]
    public void SubscriptionType_Should_Convert_From_Integer(int value, SubscriptionType expected)
    {
        // Act
        var result = (SubscriptionType)value;

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Customer", SubscriptionType.Customer)]
    [InlineData("Pos", SubscriptionType.Pos)]
    [InlineData("Distributor", SubscriptionType.Distributor)]
    [InlineData("DataAccount", SubscriptionType.DataAccount)]
    [InlineData("HalafoniCustomer", SubscriptionType.HalafoniCustomer)]
    [InlineData("Prepaid", SubscriptionType.Prepaid)]
    [InlineData("VirginPrepaidCustomer", SubscriptionType.VirginPrepaidCustomer)]
    [InlineData("VirginPostpaidCustomer", SubscriptionType.VirginPostpaidCustomer)]
    public void SubscriptionType_Should_Parse_From_String(string value, SubscriptionType expected)
    {
        // Act
        var result = Enum.Parse<SubscriptionType>(value);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("customer", SubscriptionType.Customer)]
    [InlineData("POS", SubscriptionType.Pos)]
    [InlineData("DISTRIBUTOR", SubscriptionType.Distributor)]
    public void SubscriptionType_Should_Parse_From_String_IgnoreCase(string value, SubscriptionType expected)
    {
        // Act
        var result = Enum.Parse<SubscriptionType>(value, true);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("InvalidValue")]
    [InlineData("")]
    [InlineData("NotAnEnum")]
    public void SubscriptionType_Should_Throw_On_Invalid_Parse(string value)
    {
        // Act & Assert
        Action act = () => Enum.Parse<SubscriptionType>(value);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("Customer", true, SubscriptionType.Customer)]
    [InlineData("InvalidValue", false, default(SubscriptionType))]
    [InlineData("", false, default(SubscriptionType))]
    public void SubscriptionType_Should_TryParse_Correctly(string value, bool expectedSuccess, SubscriptionType expected)
    {
        // Act
        var success = Enum.TryParse<SubscriptionType>(value, out var result);

        // Assert
        success.Should().Be(expectedSuccess);
        result.Should().Be(expected);
    }

    [Fact]
    public void SubscriptionType_Should_Be_Defined_For_All_Values()
    {
        // Arrange
        var values = Enum.GetValues<SubscriptionType>();

        // Act & Assert
        foreach (var value in values)
        {
            Enum.IsDefined(typeof(SubscriptionType), value).Should().BeTrue($"Value {value} should be defined");
        }
    }

    #endregion

    #region SubscriptionBlockStatus Tests

    [Fact]
    public void SubscriptionBlockStatus_Should_Have_Correct_Values()
    {
        // Arrange & Act & Assert
        ((int)SubscriptionBlockStatus.NO_BLOCK).Should().Be(0);
        ((int)SubscriptionBlockStatus.ALL_BLOCK).Should().Be(1);
        ((int)SubscriptionBlockStatus.CHARGED_ACTIVITY_BLOCKED).Should().Be(2);
    }

    [Fact]
    public void SubscriptionBlockStatus_Should_Have_All_Expected_Values()
    {
        // Arrange
        var expectedValues = new[]
        {
            SubscriptionBlockStatus.NO_BLOCK,
            SubscriptionBlockStatus.ALL_BLOCK,
            SubscriptionBlockStatus.CHARGED_ACTIVITY_BLOCKED
        };

        // Act
        var actualValues = Enum.GetValues<SubscriptionBlockStatus>();

        // Assert
        actualValues.Should().HaveCount(3);
        actualValues.Should().BeEquivalentTo(expectedValues);
    }

    [Theory]
    [InlineData(0, SubscriptionBlockStatus.NO_BLOCK)]
    [InlineData(1, SubscriptionBlockStatus.ALL_BLOCK)]
    [InlineData(2, SubscriptionBlockStatus.CHARGED_ACTIVITY_BLOCKED)]
    public void SubscriptionBlockStatus_Should_Convert_From_Integer(int value, SubscriptionBlockStatus expected)
    {
        // Act
        var result = (SubscriptionBlockStatus)value;

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("NO_BLOCK", SubscriptionBlockStatus.NO_BLOCK)]
    [InlineData("ALL_BLOCK", SubscriptionBlockStatus.ALL_BLOCK)]
    [InlineData("CHARGED_ACTIVITY_BLOCKED", SubscriptionBlockStatus.CHARGED_ACTIVITY_BLOCKED)]
    public void SubscriptionBlockStatus_Should_Parse_From_String(string value, SubscriptionBlockStatus expected)
    {
        // Act
        var result = Enum.Parse<SubscriptionBlockStatus>(value);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region SubscriptionStatus Tests

    [Fact]
    public void SubscriptionStatus_Should_Have_Correct_Values()
    {
        // Arrange & Act & Assert
        ((int)SubscriptionStatus.CREATED).Should().Be(0);
        ((int)SubscriptionStatus.ASSOCIATED).Should().Be(1);
        ((int)SubscriptionStatus.ACTIVE_BEFORE_FIRST_USE).Should().Be(2);
        ((int)SubscriptionStatus.ACTIVE_IN_USE).Should().Be(3);
        ((int)SubscriptionStatus.ACTIVE_COOLING).Should().Be(4);
        ((int)SubscriptionStatus.ACTIVE_COLD).Should().Be(5);
        ((int)SubscriptionStatus.ACTIVE_FROZEN).Should().Be(6);
        ((int)SubscriptionStatus.ACTIVE).Should().Be(7);
        ((int)SubscriptionStatus.INACTIVE).Should().Be(8);
    }

    [Fact]
    public void SubscriptionStatus_Should_Have_All_Expected_Values()
    {
        // Arrange
        var expectedValues = new[]
        {
            SubscriptionStatus.CREATED,
            SubscriptionStatus.ASSOCIATED,
            SubscriptionStatus.ACTIVE_BEFORE_FIRST_USE,
            SubscriptionStatus.ACTIVE_IN_USE,
            SubscriptionStatus.ACTIVE_COOLING,
            SubscriptionStatus.ACTIVE_COLD,
            SubscriptionStatus.ACTIVE_FROZEN,
            SubscriptionStatus.ACTIVE,
            SubscriptionStatus.INACTIVE
        };

        // Act
        var actualValues = Enum.GetValues<SubscriptionStatus>();

        // Assert
        actualValues.Should().HaveCount(9);
        actualValues.Should().BeEquivalentTo(expectedValues);
    }

    [Theory]
    [InlineData(0, SubscriptionStatus.CREATED)]
    [InlineData(1, SubscriptionStatus.ASSOCIATED)]
    [InlineData(2, SubscriptionStatus.ACTIVE_BEFORE_FIRST_USE)]
    [InlineData(3, SubscriptionStatus.ACTIVE_IN_USE)]
    [InlineData(4, SubscriptionStatus.ACTIVE_COOLING)]
    [InlineData(5, SubscriptionStatus.ACTIVE_COLD)]
    [InlineData(6, SubscriptionStatus.ACTIVE_FROZEN)]
    [InlineData(7, SubscriptionStatus.ACTIVE)]
    [InlineData(8, SubscriptionStatus.INACTIVE)]
    public void SubscriptionStatus_Should_Convert_From_Integer(int value, SubscriptionStatus expected)
    {
        // Act
        var result = (SubscriptionStatus)value;

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("CREATED", SubscriptionStatus.CREATED)]
    [InlineData("ASSOCIATED", SubscriptionStatus.ASSOCIATED)]
    [InlineData("ACTIVE_BEFORE_FIRST_USE", SubscriptionStatus.ACTIVE_BEFORE_FIRST_USE)]
    [InlineData("ACTIVE_IN_USE", SubscriptionStatus.ACTIVE_IN_USE)]
    [InlineData("ACTIVE_COOLING", SubscriptionStatus.ACTIVE_COOLING)]
    [InlineData("ACTIVE_COLD", SubscriptionStatus.ACTIVE_COLD)]
    [InlineData("ACTIVE_FROZEN", SubscriptionStatus.ACTIVE_FROZEN)]
    [InlineData("ACTIVE", SubscriptionStatus.ACTIVE)]
    [InlineData("INACTIVE", SubscriptionStatus.INACTIVE)]
    public void SubscriptionStatus_Should_Parse_From_String(string value, SubscriptionStatus expected)
    {
        // Act
        var result = Enum.Parse<SubscriptionStatus>(value);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region IDNumberResultStatus Tests

    [Fact]
    public void IDNumberResultStatus_Should_Have_Correct_Values()
    {
        // Arrange & Act & Assert
        ((int)IDNumberResultStatus.Valid).Should().Be(0);
        ((int)IDNumberResultStatus.Invalid).Should().Be(1);
        ((int)IDNumberResultStatus.NotFound).Should().Be(2);
    }

    [Fact]
    public void IDNumberResultStatus_Should_Have_All_Expected_Values()
    {
        // Arrange
        var expectedValues = new[]
        {
            IDNumberResultStatus.Valid,
            IDNumberResultStatus.Invalid,
            IDNumberResultStatus.NotFound
        };

        // Act
        var actualValues = Enum.GetValues<IDNumberResultStatus>();

        // Assert
        actualValues.Should().HaveCount(3);
        actualValues.Should().BeEquivalentTo(expectedValues);
    }

    [Theory]
    [InlineData(0, IDNumberResultStatus.Valid)]
    [InlineData(1, IDNumberResultStatus.Invalid)]
    [InlineData(2, IDNumberResultStatus.NotFound)]
    public void IDNumberResultStatus_Should_Convert_From_Integer(int value, IDNumberResultStatus expected)
    {
        // Act
        var result = (IDNumberResultStatus)value;

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Valid", IDNumberResultStatus.Valid)]
    [InlineData("Invalid", IDNumberResultStatus.Invalid)]
    [InlineData("NotFound", IDNumberResultStatus.NotFound)]
    public void IDNumberResultStatus_Should_Parse_From_String(string value, IDNumberResultStatus expected)
    {
        // Act
        var result = Enum.Parse<IDNumberResultStatus>(value);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Cross-Enum Validation Tests

    [Fact]
    public void All_Enums_Should_Have_Default_Zero_Values()
    {
        // Act & Assert
        ((int)default(SubscriptionType)).Should().Be(0);
        ((int)default(SubscriptionBlockStatus)).Should().Be(0);
        ((int)default(SubscriptionStatus)).Should().Be(0);
        ((int)default(IDNumberResultStatus)).Should().Be(0);
    }

    [Fact]
    public void All_Enums_Should_Have_Unique_Names()
    {
        // Arrange
        var allEnumNames = new List<string>();
        
        // Act
        allEnumNames.AddRange(Enum.GetNames<SubscriptionType>());
        allEnumNames.AddRange(Enum.GetNames<SubscriptionBlockStatus>());
        allEnumNames.AddRange(Enum.GetNames<SubscriptionStatus>());
        allEnumNames.AddRange(Enum.GetNames<IDNumberResultStatus>());

        // Assert
        allEnumNames.Should().OnlyHaveUniqueItems("All enum values should have unique names across all enums");
    }

    [Fact]
    public void All_Enums_Should_Be_Serializable()
    {
        // Arrange
        var subscriptionType = SubscriptionType.Customer;
        var blockStatus = SubscriptionBlockStatus.NO_BLOCK;
        var status = SubscriptionStatus.ACTIVE;
        var idStatus = IDNumberResultStatus.Valid;

        // Act & Assert
        subscriptionType.ToString().Should().Be("Customer");
        blockStatus.ToString().Should().Be("NO_BLOCK");
        status.ToString().Should().Be("ACTIVE");
        idStatus.ToString().Should().Be("Valid");
    }

    #endregion
} 