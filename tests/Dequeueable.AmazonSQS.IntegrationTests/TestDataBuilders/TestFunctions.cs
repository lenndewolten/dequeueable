using Dequeueable.AmazonSQS.Models;

namespace Dequeueable.AmazonSQS.IntegrationTests.TestDataBuilders
{
    public sealed class TestFunction(IFakeService fakeService) : IAmazonSQSFunction
    {
        public async Task ExecuteAsync(Message message, CancellationToken cancellationToken)
        {
            await fakeService.Execute(message);
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

    public class SingletonFakeService : IFakeService
    {
        private readonly static SemaphoreSlim _lock = new(1, 1);

        public async Task Execute(Message message)
        {

            if (await _lock.WaitAsync(TimeSpan.FromMilliseconds(1)))
            {
                await Task.Delay(10);
                _lock.Release();
                return;
            }

            throw new InvalidOperationException();
        }
    }
}
