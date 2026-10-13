import sys

filepath = 'Easy Copier/Views/AppsTabView.xaml'

with open(filepath, 'r') as f:
    content = f.read()

search_grid_str = """    <Grid>
        <ProgressRing"""

new_grid_str = """    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <AutoSuggestBox x:Name="SearchBox" Grid.Row="0" Margin="0,0,0,8"
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

        <Grid Grid.Row="2">
        <ProgressRing"""

content = content.replace(search_grid_str, new_grid_str)
content = content.replace("</UserControl>", "    </Grid>\n</UserControl>")

with open(filepath, 'w') as f:
    f.write(content)
