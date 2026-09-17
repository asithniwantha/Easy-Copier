using Easy_Copier.Infrastructure;
using System;
using System.IO;
using Xunit;

namespace Easy_Copier.Tests
{
    public class RufusResolutionHelperTests
    {
        [Theory]
        [InlineData("rufus-4.7.exe", "4.7")]
        [InlineData("rufus-4.10.exe", "4.10")]
        [InlineData("rufus-4.7p.exe", "4.7")]
        [InlineData("rufus_4.8_arm64.exe", "4.8")]
        [InlineData("rufus-3.21.1977.exe", "3.21.1977")]
        public void TryParseVersionFromFileName_ValidVersionStrings_ParsesCorrectly(string fileName, string expectedVersion)
        {
            Version? version = RufusResolutionHelper.TryParseVersionFromFileName(fileName);
            Assert.NotNull(version);
            Assert.Equal(Version.Parse(expectedVersion), version);
        }

        [Theory]
        [InlineData("rufus.exe")]
        [InlineData("rufus_setup.exe")]
        [InlineData("randomfile.exe")]
        [InlineData("")]
        [InlineData(null)]
        public void TryParseVersionFromFileName_UnversionedOrInvalid_ReturnsNull(string? fileName)
        {
            Version? version = RufusResolutionHelper.TryParseVersionFromFileName(fileName!);
            Assert.Null(version);
        }

        [Fact]
        public void ResolveLatestRufusPath_SelectsHighestVersionInSameDirectory()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "RufusTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string pathOld = Path.Combine(tempDir, "rufus-4.5.exe");
                string pathNew = Path.Combine(tempDir, "rufus-4.10.exe");
                string pathMid = Path.Combine(tempDir, "rufus-4.7.exe");
                string pathUnversioned = Path.Combine(tempDir, "rufus.exe");

                File.WriteAllText(pathOld, "dummy");
                File.WriteAllText(pathNew, "dummy");
                File.WriteAllText(pathMid, "dummy");
                File.WriteAllText(pathUnversioned, "dummy");

                string resolved = RufusResolutionHelper.ResolveLatestRufusPath(pathOld);

                Assert.Equal(pathNew, resolved);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void ResolveLatestRufusPath_NoHigherVersion_ReturnsCurrentPath()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "RufusTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string pathCurrent = Path.Combine(tempDir, "rufus-4.7.exe");
                string pathUnversioned = Path.Combine(tempDir, "rufus.exe");

                File.WriteAllText(pathCurrent, "dummy");
                File.WriteAllText(pathUnversioned, "dummy");

                string resolved = RufusResolutionHelper.ResolveLatestRufusPath(pathCurrent);

                Assert.Equal(pathCurrent, resolved);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void ResolveLatestRufusPath_NonExistentFolder_ReturnsExpandedPath()
        {
            string nonExistentPath = @"C:\NonExistentFolder_12345\rufus-4.7.exe";
            string resolved = RufusResolutionHelper.ResolveLatestRufusPath(nonExistentPath);

            Assert.Equal(nonExistentPath, resolved);
        }
    }
}
