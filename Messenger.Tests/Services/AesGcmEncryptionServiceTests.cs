using FluentAssertions;
using Messenger.Infrastructure.Services;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Messenger.Tests.Services
{
    public class AesGcmEncryptionServiceTests
    {
        private const string ValidMasterKeyBase64 = "MTIzNDU2Nzg5MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTI=";

        private static IOptions<AesGcmEncryptionOptions> CreateOptions(string masterKeyBase64)
        {
            var options = new AesGcmEncryptionOptions { MasterKeyBase64 = masterKeyBase64 };
            return Options.Create(options);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ValidMasterKey_ShouldCreateService()
        {
            var act = () => new AesGcmEncryptionService(CreateOptions(ValidMasterKeyBase64));
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_MissingMasterKey_ShouldThrowArgumentException(string? masterKey)
        {
            var act = () => new AesGcmEncryptionService(CreateOptions(masterKey!));
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Encryption:MasterKeyBase64 не задан*");
        }

        [Fact]
        public void Constructor_InvalidBase64_ShouldThrowArgumentException()
        {
            var act = () => new AesGcmEncryptionService(CreateOptions("это не base64!!!"));
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Некорректный формат MasterKeyBase64*");
        }

        [Fact]
        public void Constructor_WrongKeyLength_ShouldThrowArgumentException()
        {
            var shortKey = Convert.ToBase64String(new byte[16]);
            var act = () => new AesGcmEncryptionService(CreateOptions(shortKey));
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Master key должен быть ровно 32 байта*");
        }

        #endregion

        #region Encrypt / Decrypt Roundtrip

        [Fact]
        public void EncryptDecrypt_RoundTrip_ShouldReturnOriginalText()
        {
            var service = new AesGcmEncryptionService(CreateOptions(ValidMasterKeyBase64));
            var original = "Привет из ГУАП! Это секретное сообщение для практики DevOps";
            var encrypted = service.Encrypt(original);
            var decrypted = service.Decrypt(encrypted);

            decrypted.Should().Be(original);
            encrypted.Should().NotBe(original);
            encrypted.Should().NotBeNullOrWhiteSpace();
        }

        #endregion

        #region Encrypt Edge Cases

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Encrypt_EmptyOrNullText_ShouldReturnEmptyString(string? input)
        {
            var service = new AesGcmEncryptionService(CreateOptions(ValidMasterKeyBase64));
            var result = service.Encrypt(input);
            result.Should().Be(input ?? "");
        }

        #endregion

        #region Decrypt Edge Cases

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Decrypt_EmptyOrNullText_ShouldReturnEmptyString(string? input)
        {
            var service = new AesGcmEncryptionService(CreateOptions(ValidMasterKeyBase64));
            var result = service.Decrypt(input);
            result.Should().Be(input ?? "");
        }

        [Fact]
        public void Decrypt_InvalidBase64_ShouldThrowCryptographicException()
        {
            var service = new AesGcmEncryptionService(CreateOptions(ValidMasterKeyBase64));
            var act = () => service.Decrypt("это не валидный base64!");
            act.Should().Throw<CryptographicException>()
                .WithMessage("*Некорректный формат зашифрованных данных*");
        }

        [Fact]
        public void Decrypt_TooShortData_ShouldThrowCryptographicException()
        {
            var service = new AesGcmEncryptionService(CreateOptions(ValidMasterKeyBase64));
            var shortData = Convert.ToBase64String(new byte[20]);
            var act = () => service.Decrypt(shortData);
            act.Should().Throw<CryptographicException>()
                .WithMessage("*Слишком короткие зашифрованные данные*");
        }

        #endregion

        #region TryDecryptSafe

        [Fact]
        public void TryDecryptSafe_InvalidData_ShouldReturnSafeMessage()
        {
            var service = new AesGcmEncryptionService(CreateOptions(ValidMasterKeyBase64));
            var result = service.TryDecryptSafe("полностью битые данные!!!");
            result.Should().Be("[Сообщение зашифровано или повреждено]");
        }

        [Fact]
        public void TryDecryptSafe_ValidData_ShouldDecryptSuccessfully()
        {
            var service = new AesGcmEncryptionService(CreateOptions(ValidMasterKeyBase64));
            var original = "Тест безопасной расшифровки";
            var encrypted = service.Encrypt(original);
            var result = service.TryDecryptSafe(encrypted);
            result.Should().Be(original);
        }

        #endregion
    }
}