using SmartBox.ViewModels;

namespace SmartBox.Views
{
    public partial class AppShell : Shell
    {
        public AppShell(AppShellViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;
        }
    }
}
