public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int Id { get; }
}