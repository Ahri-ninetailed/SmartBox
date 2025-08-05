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

        private async void TreeView_ItemTapped(object sender, Syncfusion.Maui.TreeView.ItemTappedEventArgs e)
        {
            if (e.Node?.Content is SmartBox.Models.Parser.Meaning meaning)
            {
                string result = await DisplayPromptAsync(
                    "Edit Meaning",
                    "Enter new meaning:",
                    initialValue: meaning.Value);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    meaning.Value = result;
                }
            }
        }
    }
}
