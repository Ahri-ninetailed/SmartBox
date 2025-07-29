using SmartBox.ViewModels;

namespace SmartBox.Views
{
    public partial class DictionaryPage : ContentPage
    {
        public DictionaryPage(DictionaryPageViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await ((DictionaryPageViewModel)BindingContext).InitializeAsync();
        }
    }

}
