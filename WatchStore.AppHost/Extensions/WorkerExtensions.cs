namespace WatchStore.AppHost.Extensions;

internal static class WorkerExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<ProjectResource> AddWorker()
        {
            var worker = builder.AddProject<WatchStore_Worker>("watchstore-worker");

            return worker;
        }
    }
}
