using SmartBox.Interfaces;

namespace SmartBox.Services
{
    public class PromptService : IPromptService
    {
        private readonly Page _page;
        public PromptService(Page page)
        {
            _page = page;
        }

        public Task<string> ShowPromptAsync(string title, string message, string initialValue)
        {
            return _page.DisplayPromptAsync(title, message, initialValue: initialValue);
        }
    }
}
