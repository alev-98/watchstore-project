namespace WatchStore.AppHost.Extensions;

internal static class ApiExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        /// <summary>
        /// Registra l'Api, con db e blobs
        /// </summary>
        /// <returns>La risorsa</returns>
        public IResourceBuilder<ProjectResource> AddApi()
        {
            const int HealthPort = 8081;

            builder.AddAzureContainerAppEnvironment("watchstore-env");

            var api = builder.AddProject<WatchStore_Api>("watchstore-api")
                                .WithExternalHttpEndpoints()
                                .PublishAsAzureContainerApp((infra, containerApp) =>
                                {
                                    var container = containerApp.Template.Containers.Single().Value;

                                    container?.Probes.Add(new ContainerAppProbe
                                    {
                                        ProbeType = ContainerAppProbeType.Liveness,
                                        HttpGet = new ContainerAppHttpRequestInfo
                                        {
                                            Path = "/alive",
                                            Port = HealthPort,
                                            Scheme = ContainerAppHttpScheme.Http,
                                        },
                                        PeriodSeconds = 10
                                    });

                                    container?.Probes.Add(new ContainerAppProbe
                                    {
                                        ProbeType = ContainerAppProbeType.Readiness,
                                        HttpGet = new ContainerAppHttpRequestInfo
                                        {
                                            Path = "/health",
                                            Port = HealthPort,
                                            Scheme = ContainerAppHttpScheme.Http,
                                        },
                                        PeriodSeconds = 10
                                    });

                                    containerApp.Template.Scale.MinReplicas = 0;
                                    containerApp.Template.Scale.MaxReplicas = 10;
                                })
                                .WithEnvironment("HTTP_PORTS", $"8080;{HealthPort.ToString()}");

            return api;
        }
    }
}
