using Avalonia.Controls;

namespace AES.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnWorkspaceClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetTab(0);
    private void OnApkLabClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetTab(1);
    private void OnFirmwareLabClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetTab(2);
    private void OnAnalyzerClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetTab(3);
    private void OnDeviceBridgeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetTab(4);

    private void SetTab(int index)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm)
            vm.ActiveTabIndex = index;
    }
}