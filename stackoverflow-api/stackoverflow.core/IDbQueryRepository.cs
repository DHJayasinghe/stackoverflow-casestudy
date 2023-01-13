using System.Data;

namespace stackoverflow.core;

public interface IDbQueryRepository
{
    Task<List<TResult>> QueryAsync<TResult>(string spName, object parameters = null, CommandType? commandType = null);

    Task<IDictionary<string, object>> QueryAsync(string spName, object param = null, IEnumerable<MapItem> mapItems = null);
}
