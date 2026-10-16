namespace EcommerceApi.Services;

/// <summary>A request the caller can fix. Mapped to 400.</summary>
public class ValidationException(string message) : Exception(message);
