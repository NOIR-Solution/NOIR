namespace NOIR.Application.Features.Cart.Commands.MergeCart;

/// <summary>
/// Command to merge a guest cart into a user's cart (on login).
/// </summary>
public sealed record MergeCartCommand(
    string SessionId,
    string UserId);
