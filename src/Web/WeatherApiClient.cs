namespace Web;

/// <summary>
///   Provides methods for accessing weather forecast data from the API service.
/// </summary>
/// <param name="httpClient">The HTTP client used to make requests to the weather API.</param>
public class WeatherApiClient(HttpClient httpClient)
{
    /// <summary>
    ///   Gets weather forecast data asynchronously.
    /// </summary>
    /// <param name="maxItems">The maximum number of forecast items to retrieve. The default is 10.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An array of <see cref="WeatherForecast" /> objects.</returns>
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;

        await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
        {
            if (forecasts?.Count >= maxItems)
            {
                break;
            }
            if (forecast is not null)
            {
                forecasts ??= [];
                forecasts.Add(forecast);
            }
        }

        return forecasts?.ToArray() ?? [];
    }
}

/// <summary>
///   Represents a weather forecast.
/// </summary>
/// <param name="Date">The forecast date.</param>
/// <param name="TemperatureC">The temperature in Celsius.</param>
/// <param name="Summary">A summary description of the weather.</param>
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    /// <summary>
    ///   Gets the temperature in Fahrenheit.
    /// </summary>
    /// <value>
    ///   The temperature in Fahrenheit, calculated from the Celsius temperature.
    /// </value>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
