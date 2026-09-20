using Easy_Copier.Models;
using Easy_Copier.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Easy_Copier.Tests
{
    public class LibraryFilterServiceTests
    {
        private readonly LibraryFilterService _service = new();

        [Fact]
        public void FilterEntries_WithNullSource_ThrowsArgumentNullException()
        {
            _ = Assert.Throws<ArgumentNullException>(() => _service.FilterEntries(null!, "query", GameCategory.All));
        }

        [Fact]
        public void FilterEntries_WithEmptyQueryAndCategoryAll_ReturnsAllEntries()
        {
            List<GameEntry> entries =
            [
                CreateGameEntry("Halo Infinite", LibraryCategory.Game),
                CreateGameEntry("Photoshop", LibraryCategory.App),
                CreateGameEntry("Inception", LibraryCategory.TvAndFilm)
            ];

            IEnumerable<GameEntry> result = _service.FilterEntries(entries, "", GameCategory.All);

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public void FilterEntries_WithTextQuery_FiltersByNameCaseInsensitive()
        {
            List<GameEntry> entries =
            [
                CreateGameEntry("Halo Infinite", LibraryCategory.Game),
                CreateGameEntry("Halo Combat Evolved", LibraryCategory.Game),
                CreateGameEntry("Cyberpunk 2077", LibraryCategory.Game)
            ];

            IEnumerable<GameEntry> result = _service.FilterEntries(entries, "halo", GameCategory.All);

            Assert.Equal(2, result.Count());
            Assert.All(result, item => Assert.Contains("Halo", item.Name, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void FilterEntries_WithCategoryFilter_FiltersByMatchingCategory()
        {
            GameEntry actionGame = CreateGameEntry("Action Game", LibraryCategory.Game, [GameCategory.Shooter]);
            GameEntry rpgGame = CreateGameEntry("RPG Game", LibraryCategory.Game, [GameCategory.RPG]);
            List<GameEntry> entries = [actionGame, rpgGame];

            IEnumerable<GameEntry> result = _service.FilterEntries(entries, null, GameCategory.Shooter);

            Assert.Single(result);
            Assert.Equal("Action Game", result.First().Name);
        }

        [Fact]
        public void SortOsImages_ByName_SortsAscendingAndDescending()
        {
            List<GameEntry> entries =
            [
                CreateGameEntry("Windows 11", LibraryCategory.OsImage),
                CreateGameEntry("Ubuntu 24.04", LibraryCategory.OsImage),
                CreateGameEntry("Arch Linux", LibraryCategory.OsImage)
            ];

            List<GameEntry> asc = _service.SortOsImages(entries, OsImageSortOption.Name, isAscending: true).ToList();
            Assert.Equal("Arch Linux", asc[0].Name);
            Assert.Equal("Ubuntu 24.04", asc[1].Name);
            Assert.Equal("Windows 11", asc[2].Name);

            List<GameEntry> desc = _service.SortOsImages(entries, OsImageSortOption.Name, isAscending: false).ToList();
            Assert.Equal("Windows 11", desc[0].Name);
            Assert.Equal("Ubuntu 24.04", desc[1].Name);
            Assert.Equal("Arch Linux", desc[2].Name);
        }

        [Fact]
        public void SortOsImages_ByDateCreated_SortsAscendingAndDescending()
        {
            DateTime now = DateTime.Now;
            GameEntry oldImage = CreateGameEntry("Old ISO", LibraryCategory.OsImage, dateCreated: now.AddDays(-10));
            GameEntry newImage = CreateGameEntry("New ISO", LibraryCategory.OsImage, dateCreated: now);

            List<GameEntry> entries = [newImage, oldImage];

            List<GameEntry> asc = _service.SortOsImages(entries, OsImageSortOption.DateCreated, isAscending: true).ToList();
            Assert.Equal("Old ISO", asc[0].Name);
            Assert.Equal("New ISO", asc[1].Name);

            List<GameEntry> desc = _service.SortOsImages(entries, OsImageSortOption.DateCreated, isAscending: false).ToList();
            Assert.Equal("New ISO", desc[0].Name);
            Assert.Equal("Old ISO", desc[1].Name);
        }

        [Fact]
        public void SortOsImages_BySize_SortsAscendingAndDescending()
        {
            GameEntry smallImage = CreateGameEntry("Small ISO", LibraryCategory.OsImage, totalBytes: 1000);
            GameEntry largeImage = CreateGameEntry("Large ISO", LibraryCategory.OsImage, totalBytes: 50000);

            List<GameEntry> entries = [largeImage, smallImage];

            List<GameEntry> asc = _service.SortOsImages(entries, OsImageSortOption.Size, isAscending: true).ToList();
            Assert.Equal("Small ISO", asc[0].Name);
            Assert.Equal("Large ISO", asc[1].Name);

            List<GameEntry> desc = _service.SortOsImages(entries, OsImageSortOption.Size, isAscending: false).ToList();
            Assert.Equal("Large ISO", desc[0].Name);
            Assert.Equal("Small ISO", desc[1].Name);
        }

        private static GameEntry CreateGameEntry(
            string name,
            LibraryCategory category,
            List<GameCategory>? categories = null,
            long totalBytes = 100,
            DateTime? dateCreated = null)
        {
            return new GameEntry(
                Name: name,
                FolderPath: $"/dummy/{name}",
                TotalBytes: totalBytes,
                CoverImagePath: null,
                DateCreated: dateCreated ?? DateTime.Now,
                HasLargeFiles: false,
                Category: category,
                Categories: categories);
        }
    }
}
