namespace BlazorServerSentEvents.Services;

public interface IProductStockService
{
    void UpdateStock(int productId, int stock);
    IAsyncEnumerable<ProductStock> Subscribe(CancellationToken cancellationToken);
}

public record ProductStock(int ProductId, int Stock);