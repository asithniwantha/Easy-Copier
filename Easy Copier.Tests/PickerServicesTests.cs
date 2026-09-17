using Easy_Copier.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Easy_Copier.Tests
{
    public class PickerServicesTests
    {
        [Fact]
        public void FolderPickerService_Constructor_ThrowsOnNullDispatcher()
        {
            Mock<IAppWindowContext> mockContext = new();
            _ = Assert.Throws<ArgumentNullException>(() => new FolderPickerService(null!, mockContext.Object));
        }

        [Fact]
        public void FolderPickerService_Constructor_ThrowsOnNullAppWindowContext()
        {
            Mock<IDispatcherService> mockDispatcher = new();
            _ = Assert.Throws<ArgumentNullException>(() => new FolderPickerService(mockDispatcher.Object, null!));
        }

        [Fact]
        public async Task FolderPickerService_PickFolderAsync_ReturnsNullWhenDispatcherEnqueueFails()
        {
            Mock<IDispatcherService> mockDispatcher = new();
            Mock<IAppWindowContext> mockContext = new();
            Mock<ILogger<FolderPickerService>> mockLogger = new();

            _ = mockDispatcher.Setup(d => d.TryEnqueue(It.IsAny<Action>())).Returns(false);

            FolderPickerService service = new(mockDispatcher.Object, mockContext.Object, mockLogger.Object);

            string? result = await service.PickFolderAsync();

            Assert.Null(result);
            mockDispatcher.Verify(d => d.TryEnqueue(It.IsAny<Action>()), Times.Once);
        }

        [Fact]
        public async Task FolderPickerService_PickFolderAsync_HandlesMissingWindowHandleGracefully()
        {
            Mock<IDispatcherService> mockDispatcher = new();
            Mock<IAppWindowContext> mockContext = new();
            Mock<ILogger<FolderPickerService>> mockLogger = new();

            _ = mockDispatcher.Setup(d => d.TryEnqueue(It.IsAny<Action>()))
                .Callback<Action>(action => action())
                .Returns(true);

            _ = mockContext.Setup(c => c.MainWindow).Returns((object?)null);

            FolderPickerService service = new(mockDispatcher.Object, mockContext.Object, mockLogger.Object);

            string? result = await service.PickFolderAsync();

            Assert.Null(result);
        }

        [Fact]
        public void FilePickerService_Constructor_ThrowsOnNullDispatcher()
        {
            Mock<IAppWindowContext> mockContext = new();
            _ = Assert.Throws<ArgumentNullException>(() => new FilePickerService(null!, mockContext.Object));
        }

        [Fact]
        public void FilePickerService_Constructor_ThrowsOnNullAppWindowContext()
        {
            Mock<IDispatcherService> mockDispatcher = new();
            _ = Assert.Throws<ArgumentNullException>(() => new FilePickerService(mockDispatcher.Object, null!));
        }

        [Fact]
        public async Task FilePickerService_PickSaveFileAsync_ThrowsOnNullFileTypeChoices()
        {
            Mock<IDispatcherService> mockDispatcher = new();
            Mock<IAppWindowContext> mockContext = new();

            FilePickerService service = new(mockDispatcher.Object, mockContext.Object);

            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.PickSaveFileAsync("test.json", null!));
        }

        [Fact]
        public async Task FilePickerService_PickSaveFileAsync_ReturnsNullWhenDispatcherEnqueueFails()
        {
            Mock<IDispatcherService> mockDispatcher = new();
            Mock<IAppWindowContext> mockContext = new();
            Mock<ILogger<FilePickerService>> mockLogger = new();

            _ = mockDispatcher.Setup(d => d.TryEnqueue(It.IsAny<Action>())).Returns(false);

            FilePickerService service = new(mockDispatcher.Object, mockContext.Object, mockLogger.Object);
            Dictionary<string, IList<string>> fileTypes = new() { { "JSON", new List<string> { ".json" } } };

            string? result = await service.PickSaveFileAsync("test.json", fileTypes);

            Assert.Null(result);
            mockDispatcher.Verify(d => d.TryEnqueue(It.IsAny<Action>()), Times.Once);
        }

        [Fact]
        public async Task FilePickerService_PickSaveFileAsync_HandlesMissingWindowHandleGracefully()
        {
            Mock<IDispatcherService> mockDispatcher = new();
            Mock<IAppWindowContext> mockContext = new();
            Mock<ILogger<FilePickerService>> mockLogger = new();

            _ = mockDispatcher.Setup(d => d.TryEnqueue(It.IsAny<Action>()))
                .Callback<Action>(action => action())
                .Returns(true);

            _ = mockContext.Setup(c => c.MainWindow).Returns((object?)null);

            FilePickerService service = new(mockDispatcher.Object, mockContext.Object, mockLogger.Object);
            Dictionary<string, IList<string>> fileTypes = new() { { "JSON", new List<string> { ".json" } } };

            string? result = await service.PickSaveFileAsync("test.json", fileTypes);

            Assert.Null(result);
        }
    }
}
