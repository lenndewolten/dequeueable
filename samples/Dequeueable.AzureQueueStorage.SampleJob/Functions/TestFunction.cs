using Dequeueable.AzureQueueStorage.Models;
using Microsoft.Extensions.Logging;

namespace Dequeueable.AzureQueueStorage.SampleJob.Functions
{
    internal sealed class TestFunction(ILogger<TestFunction> logger) : IAzureQueueFunction
    {
        public async Task ExecuteAsync(Message message, CancellationToken cancellationToken)
        {
            for (var i = 0; i < 6; i++)
            {
                logger.LogInformation("Executing job loop {I}", i);
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("Job cancelled!");
                    break;
                }
                await Task.Delay(10000, cancellationToken);
            }
        }
    }
}
