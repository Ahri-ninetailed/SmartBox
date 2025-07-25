using SmartBox.ViewModels;

namespace SmartBox.Views
{
    public partial class ParserPage : ContentPage
    {
        public ParserPage(ParserPageViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;
        }
    }
}
