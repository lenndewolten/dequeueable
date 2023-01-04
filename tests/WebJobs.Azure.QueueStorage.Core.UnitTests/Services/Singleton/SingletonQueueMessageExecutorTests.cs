﻿using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebJobs.Azure.QueueStorage.Core.Services.Queues;
using WebJobs.Azure.QueueStorage.Core.Services.Singleton;
using WebJobs.Azure.QueueStorage.Core.UnitTests.TestDataBuilders;

namespace WebJobs.Azure.QueueStorage.Core.UnitTests.Services.Singleton
{
    public class SingletonQueueMessageExecutorTests
    {
        [Fact]
        public async Task Given_a_SingletonQueueMessageExecutor_when_ExecuteAsync_is_called_when_the_singleton_scope_is_null_then_an_InvalidOperationException_is_thrown()
        {
            // Arrange
            var message = new MessageTestDataBuilder().Build();
            var singletonLockManagerMock = new Mock<ISingletonLockManager>(MockBehavior.Strict);
            var queueMessageExecutorMock = new Mock<IQueueMessageExecutor>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<SingletonQueueMessageExecutor>>(MockBehavior.Strict);

            var singletonAttribute = new SingletonAttributeTestDataBuilder().WithScope(null!).Build();

            var sut = new SingletonQueueMessageExecutor(singletonLockManagerMock.Object, queueMessageExecutorMock.Object, loggerMock.Object, singletonAttribute); ;

            // Act
            Func<Task> act = () => sut.ExecuteAsync(message, CancellationToken.None);

            // Assert
            await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("The Singleton Scope cannot be empty when creating a scoped distributed lock");
        }

        [Fact]
        public async Task Given_a_SingletonQueueMessageExecutor_when_ExecuteAsync_is_called_with_the_scope_is_does_not_exist_in_the_body_then_a_SingletonException_is_thrown()
        {
            // Arrange
            var scope = "MyProperty";
            var message = new MessageTestDataBuilder().WithBody("{\"KeyDoesNotExist\": \"nothing here\"}").Build();
            var singletonLockManagerMock = new Mock<ISingletonLockManager>(MockBehavior.Strict);
            var queueMessageExecutorMock = new Mock<IQueueMessageExecutor>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<SingletonQueueMessageExecutor>>(MockBehavior.Strict);

            var singletonAttribute = new SingletonAttributeTestDataBuilder().WithScope(scope).Build();

            var sut = new SingletonQueueMessageExecutor(singletonLockManagerMock.Object, queueMessageExecutorMock.Object, loggerMock.Object, singletonAttribute); ;

            // Act
            Func<Task> act = () => sut.ExecuteAsync(message, CancellationToken.None);

            // Assert
            await act.Should().ThrowExactlyAsync<SingletonException>().WithMessage($"The provided scope name, '{singletonAttribute.Scope}' , does not exist on the message with id '{message.MessageId}'");
        }

        [Fact]
        public async Task Given_a_SingletonQueueMessageExecutor_when_ExecuteAsync_is_called_and_the_body_is_not_valid_JSON_then_a_SingletonException_is_thrown()
        {
            // Arrange
            var scope = "MyProperty";
            var message = new MessageTestDataBuilder().WithBody("this is no jason!").Build();
            var singletonLockManagerMock = new Mock<ISingletonLockManager>(MockBehavior.Strict);
            var queueMessageExecutorMock = new Mock<IQueueMessageExecutor>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<SingletonQueueMessageExecutor>>(MockBehavior.Strict);

            var singletonAttribute = new SingletonAttributeTestDataBuilder().WithScope(scope).Build();

            var sut = new SingletonQueueMessageExecutor(singletonLockManagerMock.Object, queueMessageExecutorMock.Object, loggerMock.Object, singletonAttribute); ;

            // Act
            Func<Task> act = () => sut.ExecuteAsync(message, CancellationToken.None);

            // Assert
            await act.Should().ThrowExactlyAsync<SingletonException>().WithMessage($"Unable to parse the body for the message with id '{message.MessageId}'");
        }

        [Fact]
        public async Task Given_a_SingletonQueueMessageExecutor_when_ExecuteAsync_is_called_and_the_parsed_property_is_empty_then_a_SingletonException_is_thrown()
        {
            // Arrange
            var scope = "MyProperty";
            var message = new MessageTestDataBuilder().WithBody("{\"MyProperty\": \"\"}").Build();
            var singletonLockManagerMock = new Mock<ISingletonLockManager>(MockBehavior.Strict);
            var queueMessageExecutorMock = new Mock<IQueueMessageExecutor>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<SingletonQueueMessageExecutor>>(MockBehavior.Strict);

            var singletonAttribute = new SingletonAttributeTestDataBuilder().WithScope(scope).Build();

            var sut = new SingletonQueueMessageExecutor(singletonLockManagerMock.Object, queueMessageExecutorMock.Object, loggerMock.Object, singletonAttribute); ;

            // Act
            Func<Task> act = () => sut.ExecuteAsync(message, CancellationToken.None);

            // Assert
            await act.Should().ThrowExactlyAsync<SingletonException>().WithMessage($"The provided scope name, '{singletonAttribute.Scope}' , does not exist on the message with id '{message.MessageId}'");
        }

        [Fact]
        public async Task Given_a_SingletonQueueMessageExecutor_when_ExecuteAsync_is_called_and_the_lease_cannot_be_renewed_then_a_SingletonException_is_thrown()
        {
            // Arrange
            var propertyName = "MyProperty";
            var scope = "this is a valid scope";
            var leaseId = "someId";
            var message = new MessageTestDataBuilder().WithBody($"{{\"{propertyName}\": \"{scope}\"}}").Build();
            var singletonLockManagerMock = new Mock<ISingletonLockManager>(MockBehavior.Strict);
            var queueMessageExecutorMock = new Mock<IQueueMessageExecutor>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<SingletonQueueMessageExecutor>>(MockBehavior.Strict);

            var singletonAttribute = new SingletonAttributeTestDataBuilder().WithScope(propertyName).WithMinimumInterval(TimeSpan.Zero).Build();
            singletonLockManagerMock.Setup(s => s.AquireLockAsync(scope, It.IsAny<CancellationToken>())).ReturnsAsync(leaseId);
            singletonLockManagerMock.Setup(s => s.ReleaseLockAsync(leaseId, scope, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            singletonLockManagerMock.Setup(s => s.RenewLockAsync(leaseId, scope, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Renew fails!"));
            queueMessageExecutorMock.Setup(s => s.ExecuteAsync(message, It.IsAny<CancellationToken>())).Returns(Task.Delay(TimeSpan.FromSeconds(10)));

            var sut = new SingletonQueueMessageExecutor(singletonLockManagerMock.Object, queueMessageExecutorMock.Object, loggerMock.Object, singletonAttribute); ;

            // Act
            Func<Task> act = () => sut.ExecuteAsync(message, CancellationToken.None);

            // Assert
            await act.Should().ThrowExactlyAsync<SingletonException>().WithMessage($"Unable to renew the lease with id '{leaseId}'. Distributed lock cannot be guaranteed.");
        }

        [Fact]
        public async Task Given_a_SingletonQueueMessageExecutor_when_ExecuteAsync_is_called_and_an_exception_occrurs_when_executing_the_message_then_it_is_rethrown()
        {
            // Arrange
            var propertyName = "MyProperty";
            var scope = "this is a valid scope";
            var leaseId = "someId";
            var message = new MessageTestDataBuilder().WithBody($"{{\"{propertyName}\": \"{scope}\"}}").Build();
            var singletonLockManagerMock = new Mock<ISingletonLockManager>(MockBehavior.Strict);
            var queueMessageExecutorMock = new Mock<IQueueMessageExecutor>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<SingletonQueueMessageExecutor>>(MockBehavior.Strict);

            var singletonAttribute = new SingletonAttributeTestDataBuilder().WithScope(propertyName).WithMinimumInterval(TimeSpan.FromSeconds(60)).Build();
            singletonLockManagerMock.Setup(s => s.AquireLockAsync(scope, It.IsAny<CancellationToken>())).ReturnsAsync(leaseId);
            singletonLockManagerMock.Setup(s => s.ReleaseLockAsync(leaseId, scope, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            singletonLockManagerMock.Setup(s => s.RenewLockAsync(leaseId, scope, It.IsAny<CancellationToken>())).ReturnsAsync(DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(60)));
            queueMessageExecutorMock.Setup(s => s.ExecuteAsync(message, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());

            var sut = new SingletonQueueMessageExecutor(singletonLockManagerMock.Object, queueMessageExecutorMock.Object, loggerMock.Object, singletonAttribute);

            // Act
            Func<Task> act = () => sut.ExecuteAsync(message, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task Given_a_SingletonQueueMessageExecutor_when_ExecuteAsync_is_called_then_the_message_is_executed_correctly()
        {
            // Arrange
            var propertyName = "MyProperty";
            var scope = "this is a valid scope";
            var leaseId = "someId";
            var message = new MessageTestDataBuilder().WithBody($"{{\"{propertyName}\": \"{scope}\"}}").Build();
            var singletonLockManagerMock = new Mock<ISingletonLockManager>(MockBehavior.Strict);
            var queueMessageExecutorMock = new Mock<IQueueMessageExecutor>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<SingletonQueueMessageExecutor>>(MockBehavior.Strict);

            var singletonAttribute = new SingletonAttributeTestDataBuilder().WithScope(propertyName).WithMinimumInterval(TimeSpan.FromSeconds(60)).Build();
            singletonLockManagerMock.Setup(s => s.AquireLockAsync(scope, It.IsAny<CancellationToken>())).ReturnsAsync(leaseId);
            singletonLockManagerMock.Setup(s => s.ReleaseLockAsync(leaseId, scope, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            singletonLockManagerMock.Setup(s => s.RenewLockAsync(leaseId, scope, It.IsAny<CancellationToken>())).ReturnsAsync(DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(60)));
            queueMessageExecutorMock.Setup(s => s.ExecuteAsync(message, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

            var sut = new SingletonQueueMessageExecutor(singletonLockManagerMock.Object, queueMessageExecutorMock.Object, loggerMock.Object, singletonAttribute);

            // Act
            await sut.ExecuteAsync(message, CancellationToken.None);

            // Assert
            queueMessageExecutorMock.Verify();
        }
    }
}
