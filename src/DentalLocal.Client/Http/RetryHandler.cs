namespace DentalLocal.Client.Http;

/// <summary>
/// Ретраи только для GET (см. промпт 42 аудита) — POST/PUT/DELETE повторять нельзя, они не идемпотентны
/// сами по себе (см. отдельно Idempotency-Key для платежей). Максимум 2 попытки, экспоненциальная
/// задержка с джиттером — без потолка попыток шторм запросов к недоступному backend только усугубляется.
/// </summary>
public sealed class RetryHandler : DelegatingHandler
{
    private const int MaxRetries = 2;
    private static readonly Random Jitter = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get)
            return await base.SendAsync(request, cancellationToken);

        Exception? lastException = null;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (attempt > 0)
            {
                var delayMs = (int)(Math.Pow(2, attempt) * 200) + Jitter.Next(0, 200);
                await Task.Delay(delayMs, cancellationToken);
            }

            // HttpRequestMessage нельзя отправить дважды (.NET бросает InvalidOperationException
            // "the request message was already sent") — на повторных попытках отправляем клон
            // оригинала, а не тот же объект; иначе сам retry ломался бы при первом же срабатывании.
            var attemptRequest = attempt == 0 ? request : CloneRequest(request);
            try
            {
                var response = await base.SendAsync(attemptRequest, cancellationToken);
                if ((int)response.StatusCode < 500 || attempt == MaxRetries)
                    return response;

                response.Dispose();
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                lastException = ex;
            }
            finally
            {
                if (attempt > 0)
                    attemptRequest.Dispose();
            }
        }

        throw lastException ?? new HttpRequestException("Запрос не удался после повторных попыток.");
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return clone;
    }
}
