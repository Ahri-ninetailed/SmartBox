namespace SmartBox.Interfaces
{
    public interface IAlertService
    {
        Task DisplayAlertAsync(string message, string title = "Error", string cancel = "Ok");
    }
}
