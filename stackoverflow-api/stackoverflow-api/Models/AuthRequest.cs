namespace stackoverflow.api;

internal record AuthRequest
{
    public string Username { get; init; }
    public string Password { get; init; }
}
