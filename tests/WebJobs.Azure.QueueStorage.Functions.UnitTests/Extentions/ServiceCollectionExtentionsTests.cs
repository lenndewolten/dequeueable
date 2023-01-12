﻿using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using WebJobs.Azure.QueueStorage.Functions.Attributes;
using WebJobs.Azure.QueueStorage.Functions.Configurations;
using WebJobs.Azure.QueueStorage.Functions.Extentions;
using WebJobs.Azure.QueueStorage.Functions.Factories;
using WebJobs.Azure.QueueStorage.Functions.Models;
using WebJobs.Azure.QueueStorage.Functions.Services.Hosts;
using WebJobs.Azure.QueueStorage.Functions.Services.Queues;

namespace WebJobs.Azure.QueueStorage.Functions.UnitTests.Extentions
{
    public class ServiceCollectionExtentionsTests
    {
        [Singleton("Id")]
        private class TestSingeltonFunction : IAzureQueueFunction
        {
            public Task ExecuteAsync(Message message, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private class TestFunction : IAzureQueueFunction
        {
            public Task ExecuteAsync(Message message, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void Given_a_Host_when_AddAzureQueueStorageListener_is_called_with_a_function_during_DI_then_all_the_host_services_are_registered_correctly()
        {
            // Arrange
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddAzureQueueStorageListener<TestFunction>();
                    services.PostConfigure<ListenerOptions>(options =>
                    {
                        options.ConnectionString = "test";
                        options.QueueName = "testqueue";
                    });
                    services.AddSingleton(_ => new Mock<IQueueClientFactory>().Object);
                    services.AddSingleton(_ => new Mock<IBlobClientFactory>().Object);
                });

            // Act
            using var host = hostBuilder.Build();
            var hostServices = host.Services.GetServices<IHostedService>();

            // Assert
            hostServices.Any(s => s.GetType() == typeof(ListenerHost)).Should().BeTrue();
        }

        [Fact]
        public void Given_a_Host_when_AddAzureQueueStorageListener_is_called_with_a_singleton_function_during_DI_then_all_the_host_services_are_registered_correctly()
        {
            // Arrange
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddAzureQueueStorageListener<TestSingeltonFunction>(options => options.NewBatchThreshold = 6);
                    services.PostConfigure<ListenerOptions>(options =>
                    {
                        options.ConnectionString = "test";
                        options.QueueName = "testqueue";
                    });
                    services.AddSingleton(_ => new Mock<IQueueClientFactory>().Object);
                    services.AddSingleton(_ => new Mock<IBlobClientFactory>().Object);
                });

            // Act
            using var host = hostBuilder.Build();
            var services = host.Services;

            // Assert
            services.GetServices<IHostedService>().Any(s => s.GetType() == typeof(ListenerHost)).Should().BeTrue();
            services.GetServices<IQueueMessageExecutor>().Should().HaveCount(2);
            services.GetRequiredService<IHostHandler>().Should().BeOfType(typeof(ListenerHostHandler));
            services.GetRequiredService<IOptions<ListenerOptions>>().Value.NewBatchThreshold.Should().Be(0);
        }

        [Fact]
        public void Given_a_Host_when_AddAzureQueueStorageJob_is_called_with_a_function_during_DI_then_all_the_host_services_are_registered_correctly()
        {
            // Arrange
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddAzureQueueStorageJob<TestFunction>();
                    services.PostConfigure((Action<Functions.Configurations.HostOptions>)(options =>
                    {
                        options.ConnectionString = "test";
                        options.QueueName = "testqueue";
                    }));
                    services.AddSingleton(_ => new Mock<IQueueClientFactory>().Object);
                    services.AddSingleton(_ => new Mock<IBlobClientFactory>().Object);
                });

            // Act
            using var host = hostBuilder.Build();
            var hostServices = host.Services.GetServices<IHostedService>();

            // Assert
            hostServices.Any(s => s.GetType() == typeof(JobHost)).Should().BeTrue();
        }

        [Fact]
        public void Given_a_Host_when_AddAzureQueueStorageJob_is_called_with_a_singleton_function_during_DI_then_all_the_host_services_are_registered_correctly()
        {
            // Arrange
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddAzureQueueStorageJob<TestSingeltonFunction>(options =>
                    {
                        options.ConnectionString = "test";
                        options.QueueName = "testqueue";
                    });
                    services.AddSingleton(_ => new Mock<IQueueClientFactory>().Object);
                    services.AddSingleton(_ => new Mock<IBlobClientFactory>().Object);
                });

            // Act
            using var host = hostBuilder.Build();
            var services = host.Services;

            // Assert
            services.GetServices<IHostedService>().Any(s => s.GetType() == typeof(JobHost)).Should().BeTrue();
            services.GetServices<IQueueMessageExecutor>().Should().HaveCount(2);
            services.GetRequiredService<IHostHandler>().Should().BeOfType(typeof(JobHostHandler));
        }
    }
}