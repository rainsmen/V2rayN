using System.Collections;

namespace ServiceLib.Helper;

public sealed class SQLiteHelper
{
    private static readonly Lazy<SQLiteHelper> _instance = new(() => new());
    public static SQLiteHelper Instance => _instance.Value;
    private readonly string _connstr;
    private SQLiteConnection _db;
    private SQLiteAsyncConnection _dbAsync;
    private readonly string _configDB = "guiNDB.db";

    public SQLiteHelper()
    {
        _connstr = Utils.GetConfigPath(_configDB);
        _db = new SQLiteConnection(_connstr, false);
        _dbAsync = new SQLiteAsyncConnection(_connstr, false);
    }

    public CreateTableResult CreateTable<T>()
    {
        var result = _db.CreateTable<T>();
        try
        {
            EnsureColumns<T>();
        }
        catch (Exception ex)
        {
            Logging.SaveLog("SQLiteHelper.EnsureColumns", ex);
        }
        return result;
    }

    /// <summary>
    /// Best-effort ALTER TABLE ADD COLUMN for pre-existing databases
    /// created before new ProfileItem/Config columns were added.
    /// sqlite-net CreateTable does not migrate existing tables.
    /// </summary>
    private void EnsureColumns<T>()
    {
        var map = _db.GetMapping<T>();
        List<SQLiteConnection.ColumnInfo> existing;
        try
        {
            existing = _db.GetTableInfo(map.TableName);
        }
        catch
        {
            return;
        }
        var existingNames = new HashSet<string>(existing.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var col in map.Columns)
        {
            if (existingNames.Contains(col.Name))
            {
                continue;
            }
            try
            {
                var decl = col.ColumnType?.Name switch
                {
                    "String" => "TEXT",
                    "Int32" or "Int64" or "Boolean" => "INTEGER",
                    _ => "TEXT",
                };
                _db.Execute($"ALTER TABLE \"{map.TableName}\" ADD COLUMN \"{col.Name}\" {decl}");
            }
            catch
            {
                // Column may exist under different casing or migration raced; ignore.
            }
        }
    }

    public async Task<int> InsertAllAsync(IEnumerable models)
    {
        return await _dbAsync.InsertAllAsync(models, runInTransaction: true).ConfigureAwait(false);
    }

    public async Task<int> InsertAsync(object model)
    {
        return await _dbAsync.InsertAsync(model);
    }

    public async Task<int> ReplaceAsync(object model)
    {
        return await _dbAsync.InsertOrReplaceAsync(model);
    }

    public async Task<int> UpdateAsync(object model)
    {
        return await _dbAsync.UpdateAsync(model);
    }

    public async Task<int> UpdateAllAsync(IEnumerable models)
    {
        return await _dbAsync.UpdateAllAsync(models, runInTransaction: true).ConfigureAwait(false);
    }

    public async Task<int> DeleteAsync(object model)
    {
        return await _dbAsync.DeleteAsync(model);
    }

    public async Task<int> DeleteAllAsync<T>()
    {
        return await _dbAsync.DeleteAllAsync<T>();
    }

    public async Task<int> ExecuteAsync(string sql)
    {
        return await _dbAsync.ExecuteAsync(sql);
    }

    public int Execute(string sql)
    {
        return _db.Execute(sql);
    }

    public async Task<List<T>> QueryAsync<T>(string sql) where T : new()
    {
        return await _dbAsync.QueryAsync<T>(sql);
    }

    public AsyncTableQuery<T> TableAsync<T>() where T : new()
    {
        return _dbAsync.Table<T>();
    }

    public async Task DisposeDbConnectionAsync()
    {
        await Task.Factory.StartNew(() =>
        {
            _db?.Close();
            _db?.Dispose();
            _db = null;

            _dbAsync?.GetConnection()?.Close();
            _dbAsync?.GetConnection()?.Dispose();
            _dbAsync = null;
        });
    }
}
