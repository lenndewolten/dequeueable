using Dequeueable.AzureQueueStorage.Models;
using Microsoft.Extensions.Logging;

namespace Dequeueable.AzureQueueStorage.SampleListener.Function
{
    internal sealed class TestFunction(ILogger<TestFunction> logger) : IAzureQueueFunction
    {
        public async Task ExecuteAsync(Message message, CancellationToken cancellationToken)
        {
            for (var i = 0; i < 6; i++)
            {
                logger.LogInformation("Executing function loop {I}", i);
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("Function cancelled!");
                    break;
                }
                await Task.Delay(10000, cancellationToken);
            }
        }
    }
}
