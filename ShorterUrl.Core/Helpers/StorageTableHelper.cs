using Microsoft.Azure.Cosmos;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using ShorterUrl.Core.Domain;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Container = Microsoft.Azure.Cosmos.Container;
using System;

namespace ShorterUrl.Core.Helpers
{
    public class StorageTableHelper
    {
        private string StorageConnectionString { get; set; }

        public StorageTableHelper() { }

        public StorageTableHelper(string storageConnectionString)
        {
            StorageConnectionString = storageConnectionString;
        }
        public CloudStorageAccount CreateStorageAccountFromConnectionString()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(this.StorageConnectionString);
            return storageAccount;
        }

        private CloudTable GetTable(string tableName)
        {
            CloudStorageAccount storageAccount = this.CreateStorageAccountFromConnectionString();
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExistsAsync();

            return table;
        }
        private CloudTable GetUrlsTable()
        {
            CloudTable table = GetTable("UrlsDetails");
            return table;
        }

        private CloudTable GetStatsTable()
        {
            CloudTable table = GetTable("ClickStats");
            return table;
        }

        public async Task<ShortUrlEntity> GetShortUrlEntity(ShortUrlEntity row)
        {
            TableOperation selOperation = TableOperation.Retrieve<ShortUrlEntity>(row.PartitionKey, row.RowKey);
            TableResult result = await GetUrlsTable().ExecuteAsync(selOperation);
            ShortUrlEntity eShortUrl = result.Result as ShortUrlEntity;
            return eShortUrl;
        }

        public async Task<List<ShortUrlEntity>> GetAllShortUrlEntities()
        {
            var tblUrls = GetUrlsTable();
            TableContinuationToken token = null;
            var lstShortUrl = new List<ShortUrlEntity>();
            do
            {
                // Retreiving all entities that are NOT the NextId entity 
                // (it's the only one in the partion "KEY")
                TableQuery<ShortUrlEntity> rangeQuery = new TableQuery<ShortUrlEntity>().Where(
                    filter: TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.NotEqual, "KEY"));

                var queryResult = await tblUrls.ExecuteQuerySegmentedAsync(rangeQuery, token);
                lstShortUrl.AddRange(queryResult.Results as List<ShortUrlEntity>);
                token = queryResult.ContinuationToken;
            } while (token != null);
            return lstShortUrl;
        }

        /// <summary>
        /// Returns the ShortUrlEntity of the <paramref name="vanity"/>
        /// </summary>
        /// <param name="vanity"></param>
        /// <returns>ShortUrlEntity</returns>
        public async Task<ShortUrlEntity> GetShortUrlEntityByVanity(string vanity)
        {
            var tblUrls = GetUrlsTable();
            TableContinuationToken token = null;
            ShortUrlEntity shortUrlEntity = null;
            do
            {
                TableQuery<ShortUrlEntity> query = new TableQuery<ShortUrlEntity>().Where(
                    filter: TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, vanity));
                var queryResult = await tblUrls.ExecuteQuerySegmentedAsync(query, token);
                shortUrlEntity = queryResult.Results.FirstOrDefault();
            } while (token != null);

            return shortUrlEntity;
        }

        public async Task SaveClickStatsEntity(ClickStatsEntity newStats)
        {
            TableOperation insOperation = TableOperation.InsertOrMerge(newStats);
            TableResult result = await GetStatsTable().ExecuteAsync(insOperation);
        }

        public async Task<ShortUrlEntity> SaveShortUrlEntity(ShortUrlEntity newShortUrl)
        {

            // serializing the collection easier on json shares
            //newShortUrl.SchedulesPropertyRaw = JsonSerializer.Serialize<List<Schedule>>(newShortUrl.Schedules);

            TableOperation insOperation = TableOperation.InsertOrMerge(newShortUrl);
            TableResult result = await GetUrlsTable().ExecuteAsync(insOperation);
            ShortUrlEntity eShortUrl = result.Result as ShortUrlEntity;
            return eShortUrl;
        }

        public async Task<bool> IfShortUrlEntityExistByVanity(string vanity)
        {
            ShortUrlEntity shortUrlEntity = await GetShortUrlEntityByVanity(vanity);
            return (shortUrlEntity != null);
        }

        public async Task<bool> IfShortUrlEntityExist(ShortUrlEntity row)
        {
            ShortUrlEntity eShortUrl = await GetShortUrlEntity(row);
            return (eShortUrl != null);
        }
        public async Task<long> GetNextTableId()
        {
            //Get current ID
            TableOperation selOperation = TableOperation.Retrieve<NextId>("1", "KEY");
            TableResult result = await GetUrlsTable().ExecuteAsync(selOperation);
            NextId entity = result.Result as NextId;

            if (entity == null)
            {
                entity = new NextId
                {
                    PartitionKey = "1",
                    RowKey = "KEY",
                    Id = 1024
                };
            }
            entity.Id++;

            //Update
            TableOperation updOperation = TableOperation.InsertOrMerge(entity);

            // Execute the operation.
            await GetUrlsTable().ExecuteAsync(updOperation);

            return entity.Id;
        }


        public async Task<ShortUrlEntity> UpdateShortUrlEntity(ShortUrlEntity urlEntity)
        {
            ShortUrlEntity originalUrl = await GetShortUrlEntity(urlEntity);
            originalUrl.Url = urlEntity.Url;
            originalUrl.Title = urlEntity.Title;
            originalUrl.SchedulesPropertyRaw = JsonSerializer.Serialize<List<ScheduleEntity>>(urlEntity.Schedules);

            return await SaveShortUrlEntity(originalUrl);
        }


        public async Task<List<ClickStatsEntity>> GetAllStatsByVanity(string vanity)
        {
            var tblUrls = GetStatsTable();
            TableContinuationToken token = null;
            var lstShortUrl = new List<ClickStatsEntity>();
            do
            {
                TableQuery<ClickStatsEntity> rangeQuery;

                if (string.IsNullOrEmpty(vanity))
                {
                    rangeQuery = new TableQuery<ClickStatsEntity>();
                }
                else
                {
                    rangeQuery = new TableQuery<ClickStatsEntity>().Where(
                    filter: TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, vanity));
                }

                var queryResult = await tblUrls.ExecuteQuerySegmentedAsync(rangeQuery, token);
                lstShortUrl.AddRange(queryResult.Results as List<ClickStatsEntity>);
                token = queryResult.ContinuationToken;
            } while (token != null);
            return lstShortUrl;
        }


        public async Task<ShortUrlEntity> ArchiveShortUrlEntity(ShortUrlEntity urlEntity)
        {
            ShortUrlEntity originalUrl = await GetShortUrlEntity(urlEntity);
            originalUrl.IsArchived = true;

            return await SaveShortUrlEntity(originalUrl);
        }
    }

    public class ClickStatsEntity : TableEntity
    {
        public ClickStatsEntity() { }

        public ClickStatsEntity(string vanity)
        {
            PartitionKey = vanity;
            RowKey = Guid.NewGuid().ToString();
            Datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public int ClickCount { get; set; }
        public string Datetime { get; set; }

    }

    public class NextId : ShortUrlEntity
    {
       public long Id { get; set; }
        // Extend properties if needed
    }
}
