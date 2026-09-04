namespace Easy_Copier.Infrastructure
{
    public interface IAppWindowContext
    {
        object? MainWindow { get; }
        object? MainXamlRoot { get; }
    }
}
