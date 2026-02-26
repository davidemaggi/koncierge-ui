namespace KonciergeUI.Kube
{
    public interface IActiveForward
    {
        int BoundPort { get; }

        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync();
        IReadOnlyCollection<string> GetLogs(int maxLines);
    }
}

