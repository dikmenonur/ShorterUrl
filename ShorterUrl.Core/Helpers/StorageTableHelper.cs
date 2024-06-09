using Microsoft.Azure.Cosmos;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Container = Microsoft.Azure.Cosmos.Container;

namespace ShorterUrl.Core.Helpers
{
    public class StorageTableHelper
    {
        private readonly string _endpointUri;
        private readonly string _primaryKey;
        private readonly CosmosClient _cosmosClient;
        private Database _database;
        private Container _urlContainer;
        private Container _statsContainer;

        public StorageTableHelper(string endpointUri, string primaryKey, string databaseId)
        {
            _endpointUri = endpointUri;
            _primaryKey = primaryKey;
            _cosmosClient = new CosmosClient(_endpointUri, _primaryKey);
            InitializeAsync(databaseId).Wait();
        }

        private async Task InitializeAsync(string databaseId)
        {
            _database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            _urlContainer = await _database.CreateContainerIfNotExistsAsync("UrlsDetails", "/partitionKey");
            _statsContainer = await _database.CreateContainerIfNotExistsAsync("ClickStats", "/partitionKey");
        }

        public async Task<ShortUrlEntity> GetShortUrlEntity(string partitionKey, string rowKey)
        {
            try
            {
                ItemResponse<ShortUrlEntity> response = await _urlContainer.ReadItemAsync<ShortUrlEntity>(rowKey, new PartitionKey(partitionKey));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<List<ShortUrlEntity>> GetAllShortUrlEntities()
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.RowKey != 'KEY'");
            var iterator = _urlContainer.GetItemQueryIterator<ShortUrlEntity>(query);
            var results = new List<ShortUrlEntity>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task<ShortUrlEntity> GetShortUrlEntityByVanity(string vanity)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.RowKey = @vanity")
                .WithParameter("@vanity", vanity);
            var iterator = _urlContainer.GetItemQueryIterator<ShortUrlEntity>(query);
            var shortUrlEntity = await iterator.ReadNextAsync();

            return shortUrlEntity.FirstOrDefault();
        }

        public async Task SaveClickStatsEntity(ClickStatsEntity newStats)
        {
            await _statsContainer.UpsertItemAsync(newStats, new PartitionKey(newStats.PartitionKey));
        }

        public async Task<ShortUrlEntity> SaveShortUrlEntity(ShortUrlEntity newShortUrl)
        {
            newShortUrl.SchedulesPropertyRaw = JsonSerializer.Serialize(newShortUrl.Schedules);
            var response = await _urlContainer.UpsertItemAsync(newShortUrl, new PartitionKey(newShortUrl.PartitionKey));
            return response.Resource;
        }

        public async Task<bool> IfShortUrlEntityExistByVanity(string vanity)
        {
            var shortUrlEntity = await GetShortUrlEntityByVanity(vanity);
            return shortUrlEntity != null;
        }

        public async Task<bool> IfShortUrlEntityExist(ShortUrlEntity row)
        {
            var eShortUrl = await GetShortUrlEntity(row.PartitionKey, row.RowKey);
            return eShortUrl != null;
        }

        public async Task<int> GetNextTableId()
        {
            var nextIdEntity = await GetShortUrlEntity("1", "KEY");

            if (nextIdEntity == null)
            {
                nextIdEntity = new ShortUrlEntity
                {
                    PartitionKey = "1",
                    RowKey = "KEY",
                    Id = 1024
                };
            }
            nextIdEntity.Id++;

            await SaveShortUrlEntity(nextIdEntity);

            return nextIdEntity.Id;
        }

        public async Task<ShortUrlEntity> UpdateShortUrlEntity(ShortUrlEntity urlEntity)
        {
            var originalUrl = await GetShortUrlEntity(urlEntity.PartitionKey, urlEntity.RowKey);
            if (originalUrl != null)
            {
                originalUrl.Url = urlEntity.Url;
                originalUrl.Title = urlEntity.Title;
                originalUrl.SchedulesPropertyRaw = JsonSerializer.Serialize(urlEntity.Schedules);
                return await SaveShortUrlEntity(originalUrl);
            }
            return null;
        }

        public async Task<List<ClickStatsEntity>> GetAllStatsByVanity(string vanity)
        {
            var query = string.IsNullOrEmpty(vanity) ?
                new QueryDefinition("SELECT * FROM c") :
                new QueryDefinition("SELECT * FROM c WHERE c.PartitionKey = @vanity")
                    .WithParameter("@vanity", vanity);

            var iterator = _statsContainer.GetItemQueryIterator<ClickStatsEntity>(query);
            var results = new List<ClickStatsEntity>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task<ShortUrlEntity> ArchiveShortUrlEntity(ShortUrlEntity urlEntity)
        {
            var originalUrl = await GetShortUrlEntity(urlEntity.PartitionKey, urlEntity.RowKey);
            if (originalUrl != null)
            {
                originalUrl.IsArchived = true;
                return await SaveShortUrlEntity(originalUrl);
            }
            return null;
        }
    }

    public class ShortUrlEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public bool IsArchived { get; set; }
        public int Id { get; set; }
        public string SchedulesPropertyRaw { get; set; }
        public List<Schedule> Schedules { get; set; }
    }

    public class ClickStatsEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public int ClickCount { get; set; }
    }

    public class Schedule
    {
        // Define properties for Schedule class
    }

    public class NextId : ShortUrlEntity
    {
        // Extend properties if needed
    }
}
