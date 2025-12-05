using System.Data;
using Calibre_net.Client.Services;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Calibre_net.Data.Calibre;


public interface IDapperContext
{
    public IDbConnection ConnectionCreate();
    public IDbConnection ConnectionCreate(string connectionStringName);

    public IDbConnection ConnectionReadCreate();
    public IDbConnection ConnectionWriteCreate();
}

[SingletonRegistration]
public class CalibreDbDapperContext : IDapperContext
{
    private readonly IConfiguration _configuration;

    public CalibreDbDapperContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection ConnectionCreate()
    {
        return OpenConnection();
    }

    public IDbConnection ConnectionCreate(string connectionString)
    {
        return OpenConnection(connectionString);
    }

    public IDbConnection ConnectionReadCreate()
    {
        return OpenConnection();
    }

    public IDbConnection ConnectionWriteCreate()
    {
        return OpenConnection();
    }

    private string GetConnectionString()
    {
        var calibreDbLocation = _configuration["calibre:database:location"] ?? throw new InvalidOperationException("Connection string 'calibre:database:location' not found.");
        var connectionString = $"Data Source={calibreDbLocation}{Path.DirectorySeparatorChar}metadata.db;mode=ReadOnly;";
        return connectionString;
    }
    private SqliteConnection OpenConnection(string connectionString = "")
    {
        if (String.IsNullOrEmpty(connectionString))
        {
            connectionString = GetConnectionString();
        }
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout = 5000;";
        command.ExecuteNonQuery();

        connection.CreateFunction("sortconcat", (Func<string, string>)SortedConcatenate);

        Dapper.SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        Dapper.SqlMapper.AddTypeHandler(new GuidHandler());
        Dapper.SqlMapper.AddTypeHandler(new TimeSpanHandler());

        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        return connection;
    }


    // [DbFunction("sortconcat")]
    public static string SortedConcatenate(string value)
        => throw new NotSupportedException();

    abstract class SqliteTypeHandler<T> : SqlMapper.TypeHandler<T>
    {
        // Parameters are converted by Microsoft.Data.Sqlite
        public override void SetValue(IDbDataParameter parameter, T? value)
            => parameter.Value = value;
    }

    class DateTimeOffsetHandler : SqliteTypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value)
            => DateTimeOffset.Parse((string)value);
    }

    class GuidHandler : SqliteTypeHandler<Guid>
    {
        public override Guid Parse(object value)
            => Guid.Parse((string)value);
    }

    class TimeSpanHandler : SqliteTypeHandler<TimeSpan>
    {
        public override TimeSpan Parse(object value)
            => TimeSpan.Parse((string)value);
    }

}