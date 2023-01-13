using Dapper;
using System.Data;
using System.Data.SqlClient;

namespace stackoverflow.core;

public sealed class DbQueryRepository : IDbQueryRepository, IDisposable
{
    private IDbConnection _conn;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(2);
    private readonly string _connectionString;

    public DbQueryRepository(string connectionString) =>
        _connectionString = connectionString ?? throw new ArgumentException("Connection string is not specified");

    public async Task<List<TResult>> QueryAsync<TResult>(string queryOrSpName, object parameters = null, CommandType? commandType = null)
    {
        using IDbConnection conn = Connection;
        return (await conn.QueryAsync<TResult>(queryOrSpName, parameters, commandType: commandType, commandTimeout: (int)_defaultTimeout.TotalSeconds)).ToList();
    }

    public async Task<IDictionary<string, object>> QueryAsync(string spName, object param = null, IEnumerable<MapItem> mapItems = null)
    {
        using IDbConnection conn = Connection;
        var multi = await conn.QueryMultipleAsync(spName, param, commandType: CommandType.StoredProcedure, commandTimeout: (int)_defaultTimeout.TotalSeconds);
        if (mapItems == null) return null;

        IDictionary<string, object> data = new Dictionary<string, object>();
        foreach (var item in mapItems)
        {
            if (item.DataRetriveType == DataRetriveType.FirstOrDefault)
            {
                var singleItem = multi.Read(item.Type).FirstOrDefault();
                data.Add(item.PropertyName, singleItem);
            }

            if (item.DataRetriveType == DataRetriveType.List)
            {
                var listItem = multi.Read(item.Type).ToList();
                data.Add(item.PropertyName, listItem);
            }
        }
        return data;
    }

    private IDbConnection Connection
    {
        get
        {
            if (_conn == null) _conn = new SqlConnection(_connectionString);
            if (_conn.State != ConnectionState.Open)
            {
                _conn.ConnectionString = _connectionString;
                _conn.Open();
            }

            return _conn;
        }
    }

    public void Dispose()
    {
        if (_conn is null) return;

        if (_conn.State == ConnectionState.Open)
            _conn.Close();

        _conn.Dispose();
    }
}