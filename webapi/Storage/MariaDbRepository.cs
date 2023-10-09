// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A storage context that stores entities in a CosmosDB container.
/// </summary>
public class MariaDbRepository<T> : IStorageContext<T> where T : class, IStorageEntity
{

    private string tableName;
    private string conn;
    /// <summary>
    /// Initializes a new instance of the CosmosDbContext class.
    /// </summary>
    public MariaDbRepository(string connectionString, string tableName)
    {
        this.conn = connectionString;
        this.tableName = tableName;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate)
    {
        using (var db = new MariaDbContext<T>(conn, tableName))
        {
            var result = db.DataSet.Where(predicate).ToList().AsEnumerable();
            return Task.FromResult(result);
        }
    }

    /// <inheritdoc/>
    public async Task CreateAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity.Id), "Entity Id cannot be null or empty.");
        }

        using (var db = new MariaDbContext<T>(conn, tableName))
        {
            await db.DataSet.AddAsync(entity);
            await db.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(T entity)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                throw new ArgumentOutOfRangeException(nameof(entity.Id), "Entity Id cannot be null or empty.");
            }

            using (var db = new MariaDbContext<T>(conn, tableName))
            {
                db.DataSet.Remove(entity);
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<T> ReadAsync(string entityId, string _)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentOutOfRangeException(nameof(entityId), "Entity Id cannot be null or empty.");
        }
        using (var db = new MariaDbContext<T>(conn, tableName))
        {
            var item = await db.DataSet.FindAsync(entityId);
            if (item == null)
            {
                throw new KeyNotFoundException($"Entity with id {entityId} not found.");
            }
            return item;
        }
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity.Id), "Entity Id cannot be null or empty.");
        }

        using (var db = new MariaDbContext<T>(conn, tableName))
        {
            if (await db.DataSet.AnyAsync(x => x.Id == entity.Id))
            {
                db.DataSet.Update(entity);
                await db.SaveChangesAsync();
            }
            else
            {
                await db.DataSet.AddAsync(entity);
                await db.SaveChangesAsync();
            }
        }
    }

   
}
