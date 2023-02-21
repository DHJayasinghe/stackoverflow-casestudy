using System.Data;

namespace stackoverflow.core;

public interface IDbQueryRepository
{
    Task ExecuteAsync(string queryOrSpName, object parameters = null, CommandType? commandType = CommandType.Text);

    Task<List<TResult>> QueryAsync<TResult>(string queryOrSpName, object parameters = null, CommandType? commandType = null);

    Task<IDictionary<string, object>> QueryAsync(string queryOrSpName, object parameters = null, CommandType? commandType = CommandType.StoredProcedure, IEnumerable<OutputResultTranform> mapItems = null);
}
