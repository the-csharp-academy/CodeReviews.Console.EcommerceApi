namespace EcommerceApi.Services;

/// <summary>A request that clashes with existing state (duplicate name, not enough stock). Mapped to 409.</summary>
public class ConflictException(string message) : Exception(message);
