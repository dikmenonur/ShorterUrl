using Microsoft.Azure.Cosmos;
using ShorterUrl.Core.Datasource;
using ShorterUrl.Core.Domain;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShorterUrl.Core
{
    public class StorageService
    {
        private readonly UrlShortenerContext _context;
        private readonly IDatabase _redisDatabase;
        private readonly string _sqlConnectionString;

        public StorageService(UrlShortenerContext context, string redisConnectionString, string sqlConnectionString)
        {
            _context = context;
            _redisDatabase = ConnectionMultiplexer.Connect(redisConnectionString).GetDatabase();
            _sqlConnectionString = sqlConnectionString;
        }

        public async Task<ShortUrlEntity> GetShortUrlEntityAsync(string partitionKey, string rowKey)
        {
            // Try getting the entity from Redis cache first
            var cacheKey = $"{partitionKey}:{rowKey}";
            var cachedEntity = await _redisDatabase.StringGetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedEntity))
            {
                return JsonSerializer.Deserialize<ShortUrlEntity>(cachedEntity);
            }

            // Get from Cosmos DB using EF Core
            var entity = await _context.ShortUrlEntities
                                       .AsNoTracking()
                                       .FirstOrDefaultAsync(e => e.PartitionKey == partitionKey && e.RowKey == rowKey);

            if (entity != null)
            {
                // Cache the entity in Redis
                await _redisDatabase.StringSetAsync(cacheKey, JsonSerializer.Serialize(entity));
            }

            return entity;
        }

        public async Task SaveShortUrlEntityAsync(ShortUrlEntity entity)
        {
            var cacheKey = $"{entity.PartitionKey}:{entity.RowKey}";

            _context.ShortUrlEntities.Update(entity);
            await _context.SaveChangesAsync();

            // Cache the entity in Redis
            await _redisDatabase.StringSetAsync(cacheKey, JsonSerializer.Serialize(entity));

            // Save to SQL database
            using (var connection = new SqlConnection(_sqlConnectionString))
            {
                var query = @"
                    IF NOT EXISTS (SELECT 1 FROM ShortUrlEntities WHERE PartitionKey = @PartitionKey AND RowKey = @RowKey)
                    BEGIN
                        INSERT INTO ShortUrlEntities (PartitionKey, RowKey, Url, Title, ShortUrl, Clicks, IsArchived, SchedulesPropertyRaw)
                        VALUES (@PartitionKey, @RowKey, @Url, @Title, @ShortUrl, @Clicks, @IsArchived, @SchedulesPropertyRaw)
                    END
                    ELSE
                    BEGIN
                        UPDATE ShortUrlEntities
                        SET Url = @Url, Title = @Title, ShortUrl = @ShortUrl, Clicks = @Clicks, IsArchived = @IsArchived, SchedulesPropertyRaw = @SchedulesPropertyRaw
                        WHERE PartitionKey = @PartitionKey AND RowKey = @RowKey
                    END";
                await connection.ExecuteAsync(query, entity);
            }
        }

        // Additional methods for updating, deleting, etc., can be implemented similarly
    }
}
