import sys

def patch_games_tab():
    filepath = 'Easy Copier/Views/GamesTabView.xaml'
    with open(filepath, 'r') as f:
        content = f.read()

    new_row_defs = """        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>"""

    content = content.replace("""        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>""", new_row_defs)

    content = content.replace("""<StackPanel Grid.Row="0" """, """<StackPanel Grid.Row="1" """)

    parts = content.split('</StackPanel>')
    parts[1] = parts[1].replace('<Grid Grid.Row="1">', '<Grid Grid.Row="2">', 1)
    content = '</StackPanel>'.join(parts)

    autosuggestbox = """        <AutoSuggestBox x:Name="SearchBox" Grid.Row="0" Margin="0,0,0,8"
                        PlaceholderText="Search games..."
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

"""
    content = content.replace(new_row_defs + "\n", new_row_defs + "\n\n" + autosuggestbox)
    with open(filepath, 'w') as f:
        f.write(content)

def patch_apps_tab():
    filepath = 'Easy Copier/Views/AppsTabView.xaml'
    with open(filepath, 'r') as f:
        content = f.read()

    search_grid_str = """    <Grid>
        <ProgressRing"""

    new_grid_str = """    <Grid>
        <Grid.RowDefinitions>
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

        <Grid Grid.Row="1">
            <ProgressRing"""

    content = content.replace(search_grid_str, new_grid_str, 1)
    # We must replace the last </Grid> added for AppsGridView
    content = content.replace("</UserControl>", "        </Grid>\n    </Grid>\n</UserControl>")

    with open(filepath, 'w') as f:
        f.write(content)

def patch_tv_os_tab(filepath, placeholder_text):
    with open(filepath, 'r') as f:
        content = f.read()

    new_row_defs = """        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>"""

    content = content.replace("""        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>""", new_row_defs, 1)

    if 'StackPanel Grid.Row="0"' in content:
        content = content.replace('StackPanel Grid.Row="0"', 'StackPanel Grid.Row="1"')

    parts = content.split('</StackPanel>')
    if len(parts) > 1:
        parts[1] = parts[1].replace('<Grid Grid.Row="1">', '<Grid Grid.Row="2">', 1)
        content = '</StackPanel>'.join(parts)

    autosuggestbox = f"""        <AutoSuggestBox x:Name="SearchBox" Grid.Row="0" Margin="0,0,0,8"
                        PlaceholderText="{placeholder_text}"
                        QueryIcon="Find"
                        FontSize="24" MinHeight="40"
                        Text="{{x:Bind ViewModel.SearchText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}}"
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

"""
    content = content.replace(new_row_defs + "\n", new_row_defs + "\n\n" + autosuggestbox, 1)

    with open(filepath, 'w') as f:
        f.write(content)


patch_games_tab()
patch_apps_tab()
patch_tv_os_tab('Easy Copier/Views/TvAndFilmsTabView.xaml', 'Search films &amp; TV...')
patch_tv_os_tab('Easy Copier/Views/OsImagesTabView.xaml', 'Search OS images...')
