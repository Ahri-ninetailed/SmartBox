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
                var wordBehavior = WordEntry.Behaviors.OfType<CommunityToolkit.Maui.Behaviors.UserStoppedTypingBehavior>().FirstOrDefault();
                if (wordBehavior != null)
                {
                    wordBehavior.Command = viewModel.SearchCommand;
                }

                var meaningBehavior = MeaningEntry.Behaviors.OfType<CommunityToolkit.Maui.Behaviors.UserStoppedTypingBehavior>().FirstOrDefault();
                if (meaningBehavior != null)
                {
                    meaningBehavior.Command = viewModel.SearchByMeaningCommand;
                }
            }
        }
    }

}
