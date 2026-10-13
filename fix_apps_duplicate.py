import sys

filepath = 'Easy Copier/Views/AppsTabView.xaml'

with open(filepath, 'r') as f:
    content = f.read()

# I see it duplicated. Because the first Grid is in the DataTemplate.
content = content.replace(
"""        <AutoSuggestBox x:Name="SearchBox" Grid.Row="0" Margin="0,0,0,8"
                        PlaceholderText="Search apps..."
                        QueryIcon="Find"
                        FontSize="24" MinHeight="40"
                        Text="{x:Bind ViewModel.SearchText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                        TextChanged="SearchBox_TextChanged"
                        SuggestionChosen="SearchBox_SuggestionChosen"
                        QuerySubmitted="SearchBox_QuerySubmitted"
                        HorizontalAlignment="Stretch">
            <AutoSuggestBox.Resources>
                <x:Double x:Key="AutoSuggestBoxDeleteButtonFontSize">24</x:Double>
                <x:Double x:Key="AutoSuggestBoxDeleteButtonMinWidth">40</x:Double>
                <x:Double x:Key="AutoSuggestBoxDeleteButtonMinHeight">40</x:Double>
                <Style TargetType="Button">
                    <Setter Property="FontSize" Value="24" />
                    <Setter Property="MinWidth" Value="40" />
                    <Setter Property="MinHeight" Value="40" />
                </Style>
            </AutoSuggestBox.Resources>
        </AutoSuggestBox>

        <Grid Grid.Row="2">""", "    <Grid>", 1)
# wait, replacing the first one restores it.
# Let's fix it by getting the file from git then re-applying properly.
