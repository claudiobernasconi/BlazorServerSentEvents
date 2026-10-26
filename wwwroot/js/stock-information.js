export function connect(dotNetHelper, url) {
    disconnect();

    const eventSource = new EventSource(url);

    eventSource.onmessage = (event) => {
        const data = JSON.parse(event.data);
        dotNetHelper.invokeMethodAsync('ReceiveStockUpdate', data.productId, data.stock);
    };

    eventSource.onerror = () => {
        eventSource.close();
    };

    window.stockEventSource = eventSource;
}

export function disconnect() {
    if (window.stockEventSource) {
        window.stockEventSource.close();
        delete window.stockEventSource;
    }
}
