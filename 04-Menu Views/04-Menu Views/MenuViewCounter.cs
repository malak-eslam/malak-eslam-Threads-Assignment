namespace MenuViews;

public sealed class MenuViewCounter
{
    public int Views;
    private readonly object _lock = new object();
    public void RegisterView()
    {
        lock (_lock)
        {
            Views++;
        }
    }
}
