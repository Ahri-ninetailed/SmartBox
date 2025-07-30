namespace SmartBox.Interfaces
{
    public interface IAlertService
    {
        Task DisplayAlertAsync(string title, string message, string cancel = "Ok");
    }
}
