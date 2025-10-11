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

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            if (BindingContext is ParserPageViewModel viewModel)
            {
                await viewModel.OnNavigatedToAsync();
            }
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (BindingContext is ParserPageViewModel viewModel)
            {
                var behavior = WordEntry.Behaviors.OfType<CommunityToolkit.Maui.Behaviors.UserStoppedTypingBehavior>().FirstOrDefault();
                if (behavior != null)
                {
                    behavior.Command = viewModel.ProcessInputCommand;
                }
            }
        }
    }
}
