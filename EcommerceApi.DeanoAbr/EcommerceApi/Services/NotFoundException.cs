namespace EcommerceApi.Services;

/// <summary>Mapped to 404.</summary>
public class NotFoundException(string message) : Exception(message);
