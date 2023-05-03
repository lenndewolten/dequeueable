﻿using Dequeueable.AzureQueueStorage.Extentions;
using Dequeueable.AzureQueueStorage.IntegrationTests.TestDataBuilders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dequeueable.AzureQueueStorage.IntegrationTests
{
    public class ListenerHostFactory<TFunction>
        where TFunction : class, IAzureQueueFunction
    {
        public readonly IHostBuilder HostBuilder;
        private readonly Action<Configurations.ListenerHostOptions>? _options = opt =>
        {
            opt.NewBatchThreshold = 0;
        };

        public ListenerHostFactory(Action<Configurations.ListenerHostOptions>? overrideOptions = null, Action<Configurations.SingletonHostOptions>? singletonHostOptions = null)
        {
            if (overrideOptions is not null)
            {
                _options += overrideOptions;
            }

            HostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    var hostBuilder = services.AddAzureQueueStorageServices<TestFunction>()
                    .RunAsListener(_options);

                    if (singletonHostOptions is not null)
                    {
                        hostBuilder.AsSingleton(singletonHostOptions);
                    }

                    services.AddTransient<IFakeService, FakeService>();
                });
        }

        public IHostBuilder ConfigureTestServices(Action<IServiceCollection> services)
        {
            HostBuilder.ConfigureServices(services);
            return HostBuilder;
        }

        public Services.Hosts.IHostExecutor Build()
        {
            var host = HostBuilder.Build();
            return host.Services.GetRequiredService<Services.Hosts.IHostExecutor>();
        }
    }
}
