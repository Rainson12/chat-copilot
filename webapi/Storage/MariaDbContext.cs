// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Orchestration;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A storage context that stores entities in a CosmosDB container.
/// </summary>
public class MariaDbContext<T> : DbContext, IStorageContext<T>, IDisposable where T : class, IStorageEntity
{

    private string tableName;
    private string conn;
    /// <summary>
    /// Initializes a new instance of the CosmosDbContext class.
    /// </summary>
    public MariaDbContext(string connectionString, string tableName)
    {
        this.conn = connectionString;
        this.tableName = tableName;
        this.Database.EnsureCreated();
        if (!this.CheckTableExists())
        {
            var createScript = this.Database.GenerateCreateScript();
            this.Database.ExecuteSqlRaw(createScript);
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql(conn, Microsoft.EntityFrameworkCore.ServerVersion.AutoDetect(this.conn));

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<T>(entity => {
            entity.ToTable(tableName);
        });
    }

    bool CheckTableExists()
    {
        try
        {
            this.DataSet.Where(s => true).Count();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }


    public DbSet<T> DataSet { get; set; }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate)
    {
        return this.DataSet.Where(predicate).AsEnumerable();
    }

    /// <inheritdoc/>
    public async Task CreateAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity.Id), "Entity Id cannot be null or empty.");
        }

        await this.DataSet.AddAsync(entity);
        await this.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity.Id), "Entity Id cannot be null or empty.");
        }

        await this.DeleteAsync(entity);
        await this.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<T> ReadAsync(string entityId, string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentOutOfRangeException(nameof(entityId), "Entity Id cannot be null or empty.");
        }
        var item = await this.DataSet.FindAsync(entityId);
        if (item == null)
        {
            throw new KeyNotFoundException($"Entity with id {entityId} not found.");
        }
        return item;
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity.Id), "Entity Id cannot be null or empty.");
        }

        if (await this.DataSet.AnyAsync(x => x.Id == entity.Id))
        {
            this.DataSet.Update(entity);
            await this.SaveChangesAsync();
        }
        else
        {
            await this.DataSet.AddAsync(entity);
            await this.SaveChangesAsync();
        }
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Dispose();
        }
    }
}
