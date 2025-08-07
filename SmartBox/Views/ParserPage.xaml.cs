using SmartBox.Models.Parser;
using SmartBox.Services;
using SmartBox.ViewModels;

namespace SmartBox.Views
{
    public partial class ParserPage : ContentPage
    {
        public ParserPage(ParserPageViewModel viewModel)
        {
            InitializeComponent();

            PromptService promptService = new PromptService(this);

            viewModel._promptService = promptService;

            BindingContext = viewModel;
        }

        private void OnItemTapped(object sender, Syncfusion.Maui.TreeView.ItemTappedEventArgs e)
        {
            if ((e.Node?.Content is Meaning meaning) && meaning.IsEnabled)
            {
                (BindingContext as ParserPageViewModel)?.EditMeaningCommand.Execute(meaning);
            }
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            if (BindingContext is ParserPageViewModel viewModel)
            {
                await viewModel.OnNavigatedToAsync();
            }
        }
    }
}
