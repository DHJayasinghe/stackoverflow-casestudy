namespace stackoverflow.api;

internal record Question
{
    public int Id { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public string Tags { get; init; }
    public DateTime AskedDateTime { get; init; }
    public int AskedById { get; init; }
    public string AskedByDisplayName { get; init; }
    public int NoOfVotes { get; init; }
}
