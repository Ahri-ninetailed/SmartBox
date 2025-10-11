using SmartBox.ViewModels;

namespace SmartBox.Views;

public partial class DefinitionsPage : ContentPage
{
    public DefinitionsPage(DefinitionsPageViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (BindingContext is DefinitionsPageViewModel viewModel)
        {
            await viewModel.OnNavigatedToAsync();
        }
    }
}