using CreditTransfer.Core.Domain.Entities;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace CreditTransfer.Core.Domain.Tests;

public class MessageTests
{
    [Fact]
    public void Message_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var message = new Message();

        // Assert
        message.Id.Should().Be(0);
        message.Key.Should().Be(string.Empty);
        message.TextEn.Should().Be(string.Empty);
        message.TextAr.Should().Be(string.Empty);
        message.IsActive.Should().BeTrue();
        message.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        message.LastModified.Should().BeNull();
    }

    [Fact]
    public void Message_PropertiesSetCorrectly()
    {
        // Arrange
        var createdDate = DateTime.UtcNow.AddHours(-1);
        var lastModified = DateTime.UtcNow;

        // Act
        var message = new Message
        {
            Id = 123,
            Key = "TRANSFER_SUCCESS",
            TextEn = "Transfer completed successfully",
            TextAr = "تم تحويل الرصيد بنجاح",
            IsActive = false,
            CreatedDate = createdDate,
            LastModified = lastModified
        };

        // Assert
        message.Id.Should().Be(123);
        message.Key.Should().Be("TRANSFER_SUCCESS");
        message.TextEn.Should().Be("Transfer completed successfully");
        message.TextAr.Should().Be("تم تحويل الرصيد بنجاح");
        message.IsActive.Should().BeFalse();
        message.CreatedDate.Should().Be(createdDate);
        message.LastModified.Should().Be(lastModified);
    }

    [Theory]
    [InlineData("TRANSFER_SUCCESS")]
    [InlineData("TRANSFER_FAILED")]
    [InlineData("INSUFFICIENT_CREDIT")]
    [InlineData("INVALID_PIN")]
    [InlineData("UNKNOWN_SUBSCRIBER")]
    public void Message_ValidMessageKeys(string messageKey)
    {
        // Arrange
        var message = new Message { Key = messageKey };

        // Act & Assert
        message.Key.Should().Be(messageKey);
        message.Key.Should().NotBeNullOrEmpty();
        message.Key.Length.Should().BeLessOrEqualTo(100);
    }

    [Fact]
    public void Message_KeyValidation_RequiredAttribute()
    {
        // Arrange
        var message = new Message();

        // Act
        var validationContext = new ValidationContext(message);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(message, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("Key"));
    }

    [Fact]
    public void Message_KeyValidation_MaxLength()
    {
        // Arrange
        var longKey = new string('A', 101); // 101 characters, exceeds 100 limit
        var message = new Message { Key = longKey };

        // Act
        var validationContext = new ValidationContext(message);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(message, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("100"));
    }

    [Fact]
    public void Message_KeyValidation_ExactMaxLength()
    {
        // Arrange
        var exactLengthKey = new string('A', 100); // Exactly 100 characters
        var message = new Message 
        { 
            Key = exactLengthKey, 
            TextEn = "English text", 
            TextAr = "Arabic text" 
        };

        // Act
        var validationContext = new ValidationContext(message);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(message, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        message.Key.Length.Should().Be(100);
    }

    [Fact]
    public void Message_TextEnValidation_RequiredAttribute()
    {
        // Arrange
        var message = new Message { Key = "TEST_KEY" };

        // Act
        var validationContext = new ValidationContext(message);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(message, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("TextEn"));
    }

    [Fact]
    public void Message_TextArValidation_RequiredAttribute()
    {
        // Arrange
        var message = new Message { Key = "TEST_KEY", TextEn = "English text" };

        // Act
        var validationContext = new ValidationContext(message);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(message, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("TextAr"));
    }

    [Theory]
    [InlineData("Transfer completed successfully", "تم تحويل الرصيد بنجاح")]
    [InlineData("Insufficient credit", "رصيد غير كافي")]
    [InlineData("Invalid PIN", "رقم سري غير صحيح")]
    [InlineData("Unknown subscriber", "مشترك غير معروف")]
    public void Message_BilingualTextValidation(string englishText, string arabicText)
    {
        // Arrange
        var message = new Message 
        { 
            Key = "TEST_KEY", 
            TextEn = englishText, 
            TextAr = arabicText 
        };

        // Act
        var validationContext = new ValidationContext(message);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(message, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        message.TextEn.Should().Be(englishText);
        message.TextAr.Should().Be(arabicText);
    }

    [Fact]
    public void Message_TextEnValidation_LongText()
    {
        // Arrange
        var longText = new string('A', 5000); // Very long text
        var message = new Message 
        { 
            Key = "TEST_KEY", 
            TextEn = longText, 
            TextAr = "Arabic text" 
        };

        // Act
        var validationContext = new ValidationContext(message);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(message, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        message.TextEn.Should().Be(longText);
        message.TextEn.Length.Should().Be(5000);
    }

    [Fact]
    public void Message_TextArValidation_LongText()
    {
        // Arrange
        var longArabicText = new string('ا', 5000); // Very long Arabic text
        var message = new Message 
        { 
            Key = "TEST_KEY", 
            TextEn = "English text", 
            TextAr = longArabicText 
        };

        // Act
        var validationContext = new ValidationContext(message);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(message, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        message.TextAr.Should().Be(longArabicText);
        message.TextAr.Length.Should().Be(5000);
    }

    [Fact]
    public void Message_IsActiveDefaultValue()
    {
        // Arrange & Act
        var message = new Message();

        // Assert
        message.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Message_IsActiveCanBeSetToFalse()
    {
        // Arrange
        var message = new Message { IsActive = false };

        // Act & Assert
        message.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Message_TimestampValidation()
    {
        // Arrange
        var message = new Message();
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Act
        var creationTime = message.CreatedDate;

        // Assert
        creationTime.Should().BeAfter(beforeCreation);
        creationTime.Should().BeBefore(afterCreation);
        message.LastModified.Should().BeNull();
    }

    [Fact]
    public void Message_LastModifiedCanBeSet()
    {
        // Arrange
        var message = new Message();
        var modificationTime = DateTime.UtcNow.AddHours(1);

        // Act
        message.LastModified = modificationTime;

        // Assert
        message.LastModified.Should().Be(modificationTime);
    }

    [Fact]
    public void Message_MessageTemplateValidation_WithParameters()
    {
        // Arrange
        var templateEn = "Transfer of {0} OMR from {1} to {2} completed successfully";
        var templateAr = "تم تحويل {0} ريال عماني من {1} إلى {2} بنجاح";
        var message = new Message 
        { 
            Key = "TRANSFER_SUCCESS_TEMPLATE", 
            TextEn = templateEn, 
            TextAr = templateAr 
        };

        // Act
        var formattedEn = string.Format(message.TextEn, "10.5", "96876325315", "96878715705");
        var formattedAr = string.Format(message.TextAr, "10.5", "96876325315", "96878715705");

        // Assert
        message.TextEn.Should().Be(templateEn);
        message.TextAr.Should().Be(templateAr);
        formattedEn.Should().Be("Transfer of 10.5 OMR from 96876325315 to 96878715705 completed successfully");
        formattedAr.Should().Be("تم تحويل 10.5 ريال عماني من 96876325315 إلى 96878715705 بنجاح");
    }

    [Fact]
    public void Message_MessageTemplateValidation_WithoutParameters()
    {
        // Arrange
        var simpleMessageEn = "Transfer completed successfully";
        var simpleMessageAr = "تم تحويل الرصيد بنجاح";
        var message = new Message 
        { 
            Key = "TRANSFER_SUCCESS_SIMPLE", 
            TextEn = simpleMessageEn, 
            TextAr = simpleMessageAr 
        };

        // Act & Assert
        message.TextEn.Should().Be(simpleMessageEn);
        message.TextAr.Should().Be(simpleMessageAr);
        message.TextEn.Should().NotContain("{");
        message.TextAr.Should().NotContain("{");
    }

    [Fact]
    public void Message_UnicodeValidation_ArabicText()
    {
        // Arrange
        var arabicText = "مرحبا بكم في خدمة تحويل الرصيد";
        var message = new Message 
        { 
            Key = "WELCOME_MESSAGE", 
            TextEn = "Welcome to credit transfer service", 
            TextAr = arabicText 
        };

        // Act & Assert
        message.TextAr.Should().Be(arabicText);
        message.TextAr.Should().NotBeNullOrEmpty();
        // Verify Arabic text contains Arabic characters
        message.TextAr.Should().MatchRegex(@"[\u0600-\u06FF]");
    }

    [Fact]
    public void Message_SpecialCharactersValidation()
    {
        // Arrange
        var textWithSpecialChars = "Amount: $10.50 (10% fee) - Success!";
        var message = new Message 
        { 
            Key = "SPECIAL_CHARS", 
            TextEn = textWithSpecialChars, 
            TextAr = "مبلغ: 10.50 ريال عماني - نجح!" 
        };

        // Act
        var validationContext = new ValidationContext(message);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(message, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        message.TextEn.Should().Be(textWithSpecialChars);
        message.TextEn.Should().Contain("$");
        message.TextEn.Should().Contain("(");
        message.TextEn.Should().Contain(")");
        message.TextEn.Should().Contain("!");
    }

    [Fact]
    public void Message_AllRequiredFieldsValid()
    {
        // Arrange
        var message = new Message 
        { 
            Key = "VALID_MESSAGE", 
            TextEn = "Valid English message", 
            TextAr = "رسالة صالحة بالعربية" 
        };

        // Act
        var validationContext = new ValidationContext(message);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(message, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void Message_MessageKeyConsistency()
    {
        // Arrange
        var transferSuccessMessage = new Message 
        { 
            Key = "TRANSFER_SUCCESS", 
            TextEn = "Transfer completed successfully", 
            TextAr = "تم تحويل الرصيد بنجاح" 
        };

        var transferFailedMessage = new Message 
        { 
            Key = "TRANSFER_FAILED", 
            TextEn = "Transfer failed", 
            TextAr = "فشل تحويل الرصيد" 
        };

        // Act & Assert
        transferSuccessMessage.Key.Should().NotBe(transferFailedMessage.Key);
        transferSuccessMessage.Key.Should().Contain("SUCCESS");
        transferFailedMessage.Key.Should().Contain("FAILED");
    }

    [Fact]
    public void Message_EmptyStringsValidation()
    {
        // Arrange
        var message = new Message 
        { 
            Key = "EMPTY_TEST", 
            TextEn = "", 
            TextAr = "" 
        };

        // Act
        var validationContext = new ValidationContext(message);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(message, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("TextEn"));
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("TextAr"));
    }
} 