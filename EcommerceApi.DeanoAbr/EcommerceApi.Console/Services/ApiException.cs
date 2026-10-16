namespace EcommerceApiConsole.Services;

/// <summary>
/// One typed exception for everything that can go wrong at the HTTP boundary, so the menu has a single
/// thing to catch and print.
/// </summary>
public class ApiException : Exception
{
    public ApiException(string message) : base(message)
    {
    }

    public ApiException(string message, Exception inner) : base(message, inner)
    {
    }
}
