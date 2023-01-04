﻿using Azure.Storage.Queues;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebJobs.Azure.QueueStorage.Core.Models;
using WebJobs.Azure.QueueStorage.Job.IntegrationTests.Fixtures;
using WebJobs.Azure.QueueStorage.Job.IntegrationTests.TestDataBuilders;

namespace WebJobs.Azure.QueueStorage.Job.IntegrationTests.Job
{
    [Collection("Azurite collection")]
    public class JobTests
    {

        private readonly QueueClientOptions _queueClientOptions = new() { MessageEncoding = QueueMessageEncoding.Base64 };

        [Fact]
        public async Task Given_a_Queue_when_is_has_two_messages_then_they_are_handled_correctly()
        {
            // Arrange
            var queueName = "testqueue1";
            var queueClient = new QueueClient(AzuriteFixture.ConnectionString, queueName, _queueClientOptions);

            var factory = new ApplicationFactory<TestFunction>(AzuriteFixture.ConnectionString, queueName);

            var fakeServiceMock = new Mock<IFakeService>();

            var messages = new[] { "message1", "message2" };

            factory.ConfigureTestServices(services =>
            {
                services.AddTransient(_ => fakeServiceMock.Object);
            });

            await queueClient.CreateAsync();

            foreach (var message in messages)
            {
                await queueClient.SendMessageAsync(message);
            }

            // Act
            var handler = factory.Build();
            await handler.HandleAsync(CancellationToken.None);

            // Assert
            var peekedMessage = await queueClient.PeekMessageAsync();
            peekedMessage.Value.Should().BeNull();

            foreach (var message in messages)
            {
                fakeServiceMock.Verify(f => f.Execute(It.Is<Message>(m => m.Body.ToString() == message)), Times.Once());
            }
        }

        [Fact]
        public async Task Given_a_QueueMessage_with_DequeueCount_1_when_an_error_occurred_while_executing_the_function_and_the_MaxDequeueCount_is_not_yet_reached_then_the_message_is_enqueued_correctly()
        {
            // Arrange
            var queueName = "testqueue2";
            var queueClient = new QueueClient(AzuriteFixture.ConnectionString, queueName, _queueClientOptions);

            var factory = new ApplicationFactory<TestFunction>(AzuriteFixture.ConnectionString, queueName, options =>
            {
                options.MaxDequeueCount = 5;
            });

            var fakeServiceMock = new Mock<IFakeService>();
            fakeServiceMock.Setup(f => f.Execute(It.IsAny<Message>())).ThrowsAsync(new Exception("Test exception"));

            factory.ConfigureTestServices(services =>
            {
                services.AddTransient(_ => fakeServiceMock.Object);
            });

            var message = "message1";
            await queueClient.CreateAsync();
            await queueClient.SendMessageAsync(message);

            // Act
            var handler = factory.Build();
            await handler.HandleAsync(CancellationToken.None);

            // Assert
            var peekedMessage = await queueClient.PeekMessageAsync();
            peekedMessage.Value.Should().NotBeNull();
            peekedMessage.Value.Body.ToString().Should().Be(message);
            peekedMessage.Value.DequeueCount.Should().Be(1);
        }

        [Fact]
        public async Task Given_a_QueueMessage_with_DequeueCount_1_when_an_error_occurred_while_executing_the_function_and_the_MaxDequeueCount_is_reached_then_the_message_is_moved_to_the_poisen_queue()
        {
            // Arrange
            var queueName = "testqueue3";
            var queueClient = new QueueClient(AzuriteFixture.ConnectionString, queueName, _queueClientOptions);
            var poisenQueueSuffix = "poison";

            var factory = new ApplicationFactory<TestFunction>(AzuriteFixture.ConnectionString, queueName, options =>
            {
                options.MaxDequeueCount = 1;
                options.PoisonQueueSuffix = poisenQueueSuffix;
            });

            var fakeServiceMock = new Mock<IFakeService>();
            fakeServiceMock.Setup(f => f.Execute(It.IsAny<Message>())).ThrowsAsync(new Exception("Test exception"));

            factory.ConfigureTestServices(services =>
            {
                services.AddTransient(_ => fakeServiceMock.Object);
            });

            var message = "message1";
            await queueClient.CreateAsync();
            await queueClient.SendMessageAsync(message);

            // Act
            var handler = factory.Build();
            await handler.HandleAsync(CancellationToken.None);

            // Assert
            var peekedMessage = await queueClient.PeekMessageAsync();
            peekedMessage.Value.Should().BeNull();

            var poisenQueueClient = new QueueClient(AzuriteFixture.ConnectionString, $"{queueName}-{poisenQueueSuffix}", _queueClientOptions);

            var peekedPoisonQueueMessage = await poisenQueueClient.PeekMessageAsync();
            peekedPoisonQueueMessage.Value.Should().NotBeNull();
            peekedPoisonQueueMessage.Value.Body.ToString().Should().Be(message);
        }
    }
}