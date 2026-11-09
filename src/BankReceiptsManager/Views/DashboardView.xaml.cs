using System.Windows.Controls;

namespace BankReceiptsManager.Views;

public partial class DashboardView : UserControl
{
    public DashboardView() => InitializeComponent();

    private async void FileHandlerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.DataContext is Models.BankReceipt receipt && DataContext is ViewModels.DashboardViewModel vm)
        {
            await vm.FileHandlerChangedCommand.ExecuteAsync(receipt);
        }
    }

    private async void ManualReference_LostFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox box && box.DataContext is Models.BankReceipt receipt && DataContext is ViewModels.DashboardViewModel vm)
        {
            await vm.ManualReferenceChangedCommand.ExecuteAsync(receipt);
        }
    }
}
