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

    public async Task ExecuteAsync(string queryOrSpName, object parameters = null, CommandType? commandType = CommandType.Text)
    {
        using IDbConnection conn = Connection;
        await conn.QueryAsync(queryOrSpName, parameters, commandType: commandType, commandTimeout: (int)_defaultTimeout.TotalSeconds);
    }

    public async Task<List<TResult>> QueryAsync<TResult>(string queryOrSpName, object parameters = null, CommandType? commandType = null)
    {
        using IDbConnection conn = Connection;
        return (await conn.QueryAsync<TResult>(queryOrSpName, parameters, commandType: commandType, commandTimeout: (int)_defaultTimeout.TotalSeconds)).ToList();
    }

    public async Task<IDictionary<string, object>> QueryAsync(string queryOrSpName, object parameters = null, CommandType? commandType = CommandType.StoredProcedure, IEnumerable<OutputResultTranform> mapItems = null)
    {
        using IDbConnection conn = Connection;
        var result = await conn.QueryMultipleAsync(queryOrSpName, parameters, commandType: commandType, commandTimeout: (int)_defaultTimeout.TotalSeconds);
        if (mapItems == null) return null;

        IDictionary<string, object> data = new Dictionary<string, object>();
        foreach (var item in mapItems)
        {
            if (item.Category == TransformCategory.Single)
            {
                var singleItem = result.Read(item.Type).FirstOrDefault();
                data.Add(item.Name, singleItem);
            }

            if (item.Category == TransformCategory.List)
            {
                var listItem = result.Read(item.Type).ToList();
                data.Add(item.Name, listItem);
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