using Messenger.Infrastructure.Services;

namespace Messenger.Tests.Services
{
    public class ValidationServiceTests
    {
        #region Hashing and Password Verification

        [Fact]
        public void HashPassword_ReturnsValidHash()
        {
            string password = "TestPassword123!";
            string hash = ValidationService.HashPassword(password);
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.StartsWith("$2", hash);
        }

        [Fact]
        public void VerifyPassword_ReturnsTrue_ForCorrectPassword()
        {
            string password = "MyStrongPass123!";
            string hash = ValidationService.HashPassword(password);
            bool result = ValidationService.VerifyPassword(password, hash);
            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_ReturnsFalse_ForWrongPassword()
        {
            string correctPassword = "MyStrongPass123!";
            string wrongPassword = "WrongPass123!";
            string hash = ValidationService.HashPassword(correctPassword);
            bool result = ValidationService.VerifyPassword(wrongPassword, hash);
            Assert.False(result);
        }

        #endregion

        #region Password Validation

        [Fact]
        public void ValidatePassword_ThrowsException_WhenPasswordIsEmpty()
        {
            Assert.Throws<Exception>(() => ValidationService.ValidatePassword(""));
            Assert.Throws<Exception>(() => ValidationService.ValidatePassword(" "));
            Assert.Throws<Exception>(() => ValidationService.ValidatePassword(null));
        }

        [Theory]
        [InlineData("Ab1!", "Пароль должен содержать от 8 до 20 символов")]
        [InlineData("ThisPasswordIsTooLong123456!", "Пароль должен содержать от 8 до 20 символов")]
        [InlineData("Short1", "Пароль должен содержать от 8 до 20 символов")]
        public void ValidatePassword_ThrowsException_WhenLengthInvalid(string password, string expectedMessage)
        {
            var ex = Assert.Throws<Exception>(() => ValidationService.ValidatePassword(password));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void ValidatePassword_ThrowsException_WhenNoUppercaseLetter()
        {
            var ex = Assert.Throws<Exception>(() => ValidationService.ValidatePassword("password123!"));
            Assert.Equal("Пароль должен содержать хотя бы одну заглавную букву (A-Z)", ex.Message);
        }

        [Fact]
        public void ValidatePassword_ThrowsException_WhenNoLowercaseLetter()
        {
            var ex = Assert.Throws<Exception>(() => ValidationService.ValidatePassword("PASSWORD123!"));
            Assert.Equal("Пароль должен содержать хотя бы одну строчную букву (a-z)", ex.Message);
        }

        [Fact]
        public void ValidatePassword_ThrowsException_WhenNoDigit()
        {
            var ex = Assert.Throws<Exception>(() => ValidationService.ValidatePassword("Password!!!"));
            Assert.Equal("Пароль должен содержать хотя бы одну цифру (0-9)", ex.Message);
        }

        [Fact]
        public void ValidatePassword_ThrowsException_WhenNoSpecialCharacter()
        {
            var ex = Assert.Throws<Exception>(() => ValidationService.ValidatePassword("Password123"));
            Assert.Equal("Пароль должен содержать хотя бы один специальный символ (например: !@#$%^&*)", ex.Message);
        }

        [Fact]
        public void ValidatePassword_ReturnsPassword_WhenAllRulesSatisfied()
        {
            string validPassword = "StrongPass123!";
            string result = ValidationService.ValidatePassword(validPassword);
            Assert.Equal(validPassword, result);
        }

        #endregion

        #region Birthdate Validation

        [Fact]
        public void ValidateBirthdate_ThrowsException_WhenDateInFuture()
        {
            var futureDate = DateTime.UtcNow.AddDays(1);
            var ex = Assert.Throws<Exception>(() => ValidationService.ValidateBirthdate(futureDate));
            Assert.Contains("Дата рождения не может быть позднее", ex.Message);
        }

        [Fact]
        public void ValidateBirthdate_ReturnsDateOnly_WhenDateIsValid()
        {
            var validDate = new DateTime(1995, 5, 15, 10, 30, 0);
            DateOnly result = ValidationService.ValidateBirthdate(validDate);
            Assert.Equal(new DateOnly(1995, 5, 15), result);
        }

        [Fact]
        public void ValidateBirthdate_ReturnsToday_WhenBirthdateIsToday()
        {
            var today = DateTime.UtcNow;
            DateOnly result = ValidationService.ValidateBirthdate(today);
            Assert.Equal(DateOnly.FromDateTime(today), result);
        }

        #endregion

        #region Email Validation

        [Theory]
        [InlineData("test@example.com")]
        [InlineData("user.name+tag@domain.co.uk")]
        [InlineData("user123@sub.domain.org")]
        [InlineData("valid_email@domain.com")]
        public void ValidateEmail_ReturnsEmail_WhenFormatIsCorrect(string email)
        {
            string result = ValidationService.ValidateEmail(email);
            Assert.Equal(email, result);
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("@domain.com")]
        [InlineData("user@")]
        [InlineData("user@.com")]
        [InlineData("user@domain")]
        [InlineData("user@domain.c")]
        [InlineData("")]
        public void ValidateEmail_ThrowsFormatException_WhenEmailIsInvalid(string email)
        {
            var ex = Assert.Throws<FormatException>(() => ValidationService.ValidateEmail(email));
            Assert.Equal("Email не соответствует формату", ex.Message);
        }

        #endregion
    }
}
