﻿
using Dequeueable.AmazonSQS.Models;

namespace Dequeueable.AmazonSQS
{
    /// <summary>
    /// Interface to bind a function to the framework
    /// </summary>
    public interface IAmazonSQSFunction
    {
        /// <summary>
        /// Interface that binds the class that will be invoked when a message is retrieved from the queue
        /// </summary>
        /// <param name="message">
        ///  The Queue Message on the queue
        /// </param>
        /// <param name="cancellationToken">
        /// <see cref="CancellationToken"/> to propagate
        /// notifications that the operation should be cancelled.
        /// </param>
        Task ExecuteAsync(Message message, CancellationToken cancellationToken);
    }
}
