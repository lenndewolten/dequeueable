using Dequeueable.AzureQueueStorage.Models;

namespace Dequeueable.AzureQueueStorage.IntegrationTests.TestDataBuilders
{
    public sealed class TestFunction(IFakeService fakeService) : IAzureQueueFunction
    {
        public Task ExecuteAsync(Message message, CancellationToken cancellationToken)
        {
            return fakeService.Execute(message);
        }
    }

    public interface IFakeService
    {
        Task Execute(Message message);
    }

    public class FakeService : IFakeService
    {

        public Task Execute(Message message) { return Task.CompletedTask; }
    }
}
