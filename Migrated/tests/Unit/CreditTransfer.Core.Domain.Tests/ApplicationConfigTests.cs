using CreditTransfer.Core.Domain.Entities;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace CreditTransfer.Core.Domain.Tests;

public class ApplicationConfigTests
{
    [Fact]
    public void ApplicationConfig_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new ApplicationConfig();

        // Assert
        config.Id.Should().Be(0);
        config.Key.Should().Be(string.Empty);
        config.Value.Should().Be(string.Empty);
        config.Description.Should().BeNull();
        config.Note.Should().BeNull();
        config.Category.Should().BeNull();
        config.IsActive.Should().BeTrue();
        config.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        config.LastModified.Should().BeNull();
        config.CreatedBy.Should().BeNull();
        config.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void ApplicationConfig_PropertiesSetCorrectly()
    {
        // Arrange
        var createdDate = DateTime.UtcNow.AddHours(-1);
        var lastModified = DateTime.UtcNow;

        // Act
        var config = new ApplicationConfig
        {
            Id = 123,
            Key = "TestKey",
            Value = "TestValue",
            Description = "Test Description",
            Note = "Test Note",
            Category = "TestCategory",
            IsActive = false,
            CreatedDate = createdDate,
            LastModified = lastModified,
            CreatedBy = "TestUser",
            ModifiedBy = "TestModifier"
        };

        // Assert
        config.Id.Should().Be(123);
        config.Key.Should().Be("TestKey");
        config.Value.Should().Be("TestValue");
        config.Description.Should().Be("Test Description");
        config.Note.Should().Be("Test Note");
        config.Category.Should().Be("TestCategory");
        config.IsActive.Should().BeFalse();
        config.CreatedDate.Should().Be(createdDate);
        config.LastModified.Should().Be(lastModified);
        config.CreatedBy.Should().Be("TestUser");
        config.ModifiedBy.Should().Be("TestModifier");
    }

    [Theory]
    [InlineData("AuthenticatedUserGroup")]
    [InlineData("CurrencyUnitConversion")]
    [InlineData("MaxTransferAmount")]
    [InlineData("MinTransferAmount")]
    [InlineData("DailyTransferLimit")]
    public void ApplicationConfig_ValidKeyNames(string keyName)
    {
        // Arrange
        var config = new ApplicationConfig { Key = keyName };

        // Act & Assert
        config.Key.Should().Be(keyName);
        config.Key.Should().NotBeNullOrEmpty();
        config.Key.Length.Should().BeLessOrEqualTo(200);
    }

    [Fact]
    public void ApplicationConfig_KeyValidation_RequiredAttribute()
    {
        // Arrange
        var config = new ApplicationConfig();

        // Act
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("Key"));
    }

    [Fact]
    public void ApplicationConfig_KeyValidation_MaxLength()
    {
        // Arrange
        var longKey = new string('A', 201); // 201 characters, exceeds 200 limit
        var config = new ApplicationConfig { Key = longKey };

        // Act
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("200"));
    }

    [Fact]
    public void ApplicationConfig_KeyValidation_ExactMaxLength()
    {
        // Arrange
        var exactLengthKey = new string('A', 200); // Exactly 200 characters
        var config = new ApplicationConfig { Key = exactLengthKey, Value = "TestValue" };

        // Act
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        config.Key.Length.Should().Be(200);
    }

    [Fact]
    public void ApplicationConfig_ValueValidation_RequiredAttribute()
    {
        // Arrange
        var config = new ApplicationConfig { Key = "TestKey" };

        // Act
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("Value"));
    }

    [Theory]
    [InlineData("simple_value")]
    [InlineData("complex;value;with;semicolons")]
    [InlineData("123.456")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("DefaultOperatorCreditTransferIN")]
    public void ApplicationConfig_ValueValidation_ValidValues(string value)
    {
        // Arrange
        var config = new ApplicationConfig { Key = "TestKey", Value = value };

        // Act
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        config.Value.Should().Be(value);
    }

    [Fact]
    public void ApplicationConfig_DescriptionValidation_MaxLength()
    {
        // Arrange
        var longDescription = new string('A', 1001); // 1001 characters, exceeds 1000 limit
        var config = new ApplicationConfig 
        { 
            Key = "TestKey", 
            Value = "TestValue", 
            Description = longDescription 
        };

        // Act
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public void ApplicationConfig_DescriptionValidation_ExactMaxLength()
    {
        // Arrange
        var exactLengthDescription = new string('A', 1000); // Exactly 1000 characters
        var config = new ApplicationConfig 
        { 
            Key = "TestKey", 
            Value = "TestValue", 
            Description = exactLengthDescription 
        };

        // Act
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        config.Description!.Length.Should().Be(1000);
    }

    [Fact]
    public void ApplicationConfig_CategoryValidation_MaxLength()
    {
        // Arrange
        var longCategory = new string('A', 101); // 101 characters, exceeds 100 limit
        var config = new ApplicationConfig 
        { 
            Key = "TestKey", 
            Value = "TestValue", 
            Category = longCategory 
        };

        // Act
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("100"));
    }

    [Theory]
    [InlineData("Authentication")]
    [InlineData("Transfer")]
    [InlineData("SMS")]
    [InlineData("General")]
    [InlineData("Security")]
    public void ApplicationConfig_CategoryValidation_ValidCategories(string category)
    {
        // Arrange
        var config = new ApplicationConfig 
        { 
            Key = "TestKey", 
            Value = "TestValue", 
            Category = category 
        };

        // Act
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        config.Category.Should().Be(category);
    }

    [Fact]
    public void ApplicationConfig_CreatedByValidation_MaxLength()
    {
        // Arrange
        var longCreatedBy = new string('A', 101); // 101 characters, exceeds 100 limit
        var config = new ApplicationConfig 
        { 
            Key = "TestKey", 
            Value = "TestValue", 
            CreatedBy = longCreatedBy 
        };

        // Act
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("100"));
    }

    [Fact]
    public void ApplicationConfig_ModifiedByValidation_MaxLength()
    {
        // Arrange
        var longModifiedBy = new string('A', 101); // 101 characters, exceeds 100 limit
        var config = new ApplicationConfig 
        { 
            Key = "TestKey", 
            Value = "TestValue", 
            ModifiedBy = longModifiedBy 
        };

        // Act
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("100"));
    }

    [Fact]
    public void ApplicationConfig_IsActiveDefaultValue()
    {
        // Arrange & Act
        var config = new ApplicationConfig();

        // Assert
        config.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ApplicationConfig_IsActiveCanBeSetToFalse()
    {
        // Arrange
        var config = new ApplicationConfig { IsActive = false };

        // Act & Assert
        config.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ApplicationConfig_TimestampValidation()
    {
        // Arrange
        var config = new ApplicationConfig();
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Act
        var creationTime = config.CreatedDate;

        // Assert
        creationTime.Should().BeAfter(beforeCreation);
        creationTime.Should().BeBefore(afterCreation);
        config.LastModified.Should().BeNull();
    }

    [Fact]
    public void ApplicationConfig_LastModifiedCanBeSet()
    {
        // Arrange
        var config = new ApplicationConfig();
        var modificationTime = DateTime.UtcNow.AddHours(1);

        // Act
        config.LastModified = modificationTime;

        // Assert
        config.LastModified.Should().Be(modificationTime);
    }

    [Fact]
    public void ApplicationConfig_ConfigurationHierarchyValidation()
    {
        // Arrange
        var parentConfig = new ApplicationConfig 
        { 
            Key = "ParentKey", 
            Value = "ParentValue", 
            Category = "Parent" 
        };
        
        var childConfig = new ApplicationConfig 
        { 
            Key = "ParentKey.ChildKey", 
            Value = "ChildValue", 
            Category = "Parent" 
        };

        // Act & Assert
        parentConfig.Key.Should().Be("ParentKey");
        childConfig.Key.Should().Be("ParentKey.ChildKey");
        childConfig.Key.Should().StartWith(parentConfig.Key);
        parentConfig.Category.Should().Be(childConfig.Category);
    }

    [Fact]
    public void ApplicationConfig_UniqueKeyValidation()
    {
        // Arrange
        var config1 = new ApplicationConfig { Key = "UniqueKey", Value = "Value1" };
        var config2 = new ApplicationConfig { Key = "UniqueKey", Value = "Value2" };

        // Act & Assert
        config1.Key.Should().Be(config2.Key);
        // In a real scenario, this would be handled by database constraints
        // and repository layer to ensure uniqueness
    }

    [Fact]
    public void ApplicationConfig_NoteFieldValidation()
    {
        // Arrange
        var longNote = new string('A', 5000); // Very long note
        var config = new ApplicationConfig 
        { 
            Key = "TestKey", 
            Value = "TestValue", 
            Note = longNote 
        };

        // Act & Assert
        config.Note.Should().Be(longNote);
        config.Note.Length.Should().Be(5000);
        // Note field is nvarchar(max) so it should accept long text
    }

    [Fact]
    public void ApplicationConfig_AllRequiredFieldsValid()
    {
        // Arrange
        var config = new ApplicationConfig 
        { 
            Key = "ValidKey", 
            Value = "ValidValue" 
        };

        // Act
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }
} 