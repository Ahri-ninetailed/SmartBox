using SmartBox.ViewModels;

namespace SmartBox.Views
{
    public partial class DictionaryPage : ContentPage
    {
        public DictionaryPage(DictionaryPageViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;

            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object? sender, EventArgs e)
        {
            Loaded -= OnPageLoaded;

            await ((DictionaryPageViewModel)BindingContext).InitializeAsync();
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (BindingContext is DictionaryPageViewModel viewModel)
            {
                var behavior = WordEntry.Behaviors.OfType<CommunityToolkit.Maui.Behaviors.UserStoppedTypingBehavior>().FirstOrDefault();
                if (behavior != null)
                {
                    behavior.Command = viewModel.SearchCommand;
                }
            }
        }
    }

}
