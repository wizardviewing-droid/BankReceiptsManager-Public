using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BankReceiptsManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private object? currentView;
    [ObservableProperty] private string statusMessage = "Ready";
    [ObservableProperty] private bool isProcessing;

    public DashboardViewModel Dashboard { get; }
    public ImportViewModel Import { get; }
    public ReviewViewModel Review { get; }
    public SettingsViewModel Settings { get; }

    public MainViewModel()
    {
        Dashboard = new DashboardViewModel(this);
        Import = new ImportViewModel(this);
        Review = new ReviewViewModel(this);
        Settings = new SettingsViewModel();
        CurrentView = Dashboard;
    }

    [RelayCommand]
    private void NavigateDashboard() => CurrentView = Dashboard;

    [RelayCommand]
    private void NavigateImport() => CurrentView = Import;

    [RelayCommand]
    private void NavigateReview() => CurrentView = Review;

    [RelayCommand]
    private void NavigateSettings() => CurrentView = Settings;

    public void SetStatus(string message) => StatusMessage = message;
}
