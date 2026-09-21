using Easy_Copier.Models;
using Easy_Copier.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Easy_Copier.Tests
{
    public class RufusServiceTests
    {
        private readonly Mock<ISettingsService> _mockSettingsService;
        private readonly Mock<ILogger<RufusService>> _mockLogger;
        private readonly RufusService _rufusService;

        public RufusServiceTests()
        {
            _mockSettingsService = new Mock<ISettingsService>();
            _mockLogger = new Mock<ILogger<RufusService>>();
            _rufusService = new RufusService(_mockSettingsService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task LaunchWithIsoAsync_EmptyIsoPath_ReturnsFailure()
        {
            (bool success, string message) = await _rufusService.LaunchWithIsoAsync(string.Empty);

            Assert.False(success);
            Assert.Equal("No ISO file specified.", message);
        }

        [Fact]
        public async Task LaunchWithIsoAsync_NonExistentIsoPath_ReturnsFailure()
        {
            string nonExistentPath = @"C:\NonExistentFolder_12345\image.iso";
            (bool success, string message) = await _rufusService.LaunchWithIsoAsync(nonExistentPath);

            Assert.False(success);
            Assert.Contains("ISO file not found", message);
        }
    }
}
