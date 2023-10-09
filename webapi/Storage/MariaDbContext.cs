// Copyright (c) Microsoft. All rights reserved.


using Microsoft.EntityFrameworkCore;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A storage context that stores entities in a CosmosDB container.
/// </summary>
public class MariaDbContext<T> : DbContext where T : class, IStorageEntity
{

    private string tableName;
    private string conn;
    /// <summary>
    /// Initializes a new instance of the MariaDbContext class.
    /// </summary>
    public MariaDbContext(string connectionString, string tableName)
    {
        this.conn = connectionString;
        this.tableName = tableName;
        //this.Database.EnsureCreated();
        //if (!this.CheckTableExists())
        //{
        //    var createScript = this.Database.GenerateCreateScript();
        //    this.Database.ExecuteSqlRaw(createScript);
        //}
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql(conn, Microsoft.EntityFrameworkCore.ServerVersion.AutoDetect(this.conn));

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<T>(entity =>
        {
            entity.ToTable(tableName);
        });
    }



    public DbSet<T> DataSet { get; set; }
}
