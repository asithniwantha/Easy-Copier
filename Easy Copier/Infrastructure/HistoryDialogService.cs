using Easy_Copier.Services;
using Easy_Copier.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Easy_Copier.Infrastructure
{
    public interface IHistoryDialogService
    {
        Task ShowHistoryDialogAsync();
    }

    public class HistoryDialogService : IHistoryDialogService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAppWindowContext _appWindowContext;

        public HistoryDialogService(IDatabaseService databaseService, IAppWindowContext appWindowContext)
        {
            _databaseService = databaseService;
            _appWindowContext = appWindowContext;
        }

        public async Task ShowHistoryDialogAsync()
        {
            List<SmartAdderHistoryRecord> records = await _databaseService.GetRecentRecordsAsync(50);

            var itemsList = new List<HistoryPresentationItem>();
            foreach(var r in records)
            {
                itemsList.Add(new HistoryPresentationItem
                {
                    TimestampDisplay = r.Timestamp.ToString("g", CultureInfo.CurrentCulture),
                    TotalSumDisplay = r.TotalSum.ToString("0.##", CultureInfo.CurrentCulture),
                    Values = JsonSerializer.Deserialize<List<double>>(r.EntriesJson) ?? new List<double>()
                });
            }

            var xaml = @"
            <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                          xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                <Button Background=""Transparent"" BorderThickness=""0"" HorizontalAlignment=""Stretch"" HorizontalContentAlignment=""Stretch"">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""*""/>
                            <ColumnDefinition Width=""Auto""/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text=""{Binding TimestampDisplay}"" VerticalAlignment=""Center""/>
                        <TextBlock Grid.Column=""1"" Text=""{Binding TotalSumDisplay}"" FontWeight=""Bold"" VerticalAlignment=""Center""/>
                    </Grid>
                    <Button.Flyout>
                        <Flyout>
                            <ScrollViewer MaxHeight=""300"">
                                <ItemsControl ItemsSource=""{Binding Values}"">
                                    <ItemsControl.ItemTemplate>
                                        <DataTemplate>
                                            <TextBlock Text=""{Binding}"" Margin=""0,2,0,2""/>
                                        </DataTemplate>
                                    </ItemsControl.ItemTemplate>
                                </ItemsControl>
                            </ScrollViewer>
                        </Flyout>
                    </Button.Flyout>
                </Button>
            </DataTemplate>";

            var dataTemplate = (DataTemplate)XamlReader.Load(xaml);

            var listView = new ListView
            {
                ItemsSource = itemsList,
                ItemTemplate = dataTemplate,
                SelectionMode = ListViewSelectionMode.None,
                MaxHeight = 400
            };

            var dialog = new ContentDialog
            {
                Title = "Smart Adder History",
                Content = listView,
                CloseButtonText = "Close",
                XamlRoot = _appWindowContext.MainXamlRoot as XamlRoot
            };

            if (dialog.XamlRoot != null)
            {
                await dialog.ShowAsync();
            }
        }
    }

    public class HistoryPresentationItem
    {
        public string TimestampDisplay { get; set; } = string.Empty;
        public string TotalSumDisplay { get; set; } = string.Empty;
        public List<double> Values { get; set; } = new();
    }
}
