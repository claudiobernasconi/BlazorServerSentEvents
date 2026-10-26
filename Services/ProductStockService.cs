using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace BlazorServerSentEvents.Services;

public class ProductStockService : IProductStockService
{
    private readonly ConcurrentDictionary<int, int> _stockInformation = new();
    private readonly ConcurrentDictionary<Guid, Channel<ProductStock>> _subscribers = new();

    public void UpdateStock(int productId, int stock)
    {
        _stockInformation[productId] = stock;

        var productStock = new ProductStock(productId, stock);
        foreach (var subscriber in _subscribers.Values)
        {
            subscriber.Writer.TryWrite(productStock);
        }
    }

    public async IAsyncEnumerable<ProductStock> Subscribe([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<ProductStock>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var subscriptionId = Guid.NewGuid();
        _subscribers[subscriptionId] = channel;

        try
        {
            // Send the current snapshot immediately
            foreach (var item in _stockInformation)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                yield return new ProductStock(item.Key, item.Value);
            }

            // Then keep streaming new updates
            while (!cancellationToken.IsCancellationRequested)
            {
                ProductStock? productStock;

                try
                {
                    productStock = await channel.Reader.ReadAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                yield return productStock;
            }
        }
        finally
        {
            _subscribers.TryRemove(subscriptionId, out _);
            channel.Writer.TryComplete();
        }
    }
}
