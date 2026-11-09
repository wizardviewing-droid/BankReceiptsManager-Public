using CommunityToolkit.Mvvm.ComponentModel;

namespace BankReceiptsManager.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty] private int totalImported;
    [ObservableProperty] private decimal totalValue;
    [ObservableProperty] private int matchedCount;
    [ObservableProperty] private int reviewCount;
    [ObservableProperty] private string lastImport = "Never";
}
