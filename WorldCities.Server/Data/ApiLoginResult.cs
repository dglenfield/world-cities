namespace WorldCities.Server.Data;

public class ApiLoginResult
{
    /// <summary>
    /// TRUE if the login attempt is successful. Otherwise FALSE.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Login attempt result message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// The JWT token if the login attempt is successful. Otherwise NULL.
    /// </summary>
    public string? Token { get; set; }
}
