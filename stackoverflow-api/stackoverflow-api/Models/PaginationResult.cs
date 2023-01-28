namespace stackoverflow.api;

internal record PaginationResult<T> where T : class
{
    public int TotalRecords { get; init; }
    public int FilteredRecords => Data.Count;
    public List<T> Data { get; init; }
}