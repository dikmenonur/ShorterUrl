using Azure;
using Cronos;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShorterUrl.Core.Domain
{
    public class ScheduleEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public DateTime Start { get; set; } = DateTime.Now.AddMonths(-6);
        public DateTime End { get; set; } = DateTime.Now.AddMonths(6);

        public string AlternativeUrl { get; set; } = "";
        public string Cron { get; set; } = "* * * * *";

        public int DurationMinutes { get; set; } = 0;
        string ITableEntity.ETag { get; set; }
        DateTimeOffset ITableEntity.Timestamp {get; set; }

    public string GetDisplayableUrl(int max)
        {
            var length = AlternativeUrl.ToString().Length;
            if (length >= max)
            {
                return string.Concat(AlternativeUrl.Substring(0, max - 1), "...");
            }
            return AlternativeUrl;
        }

        public bool IsActive(DateTime pointInTime)
        {
            var bufferStart = pointInTime.AddMinutes(-DurationMinutes);
            var expires = pointInTime.AddMinutes(DurationMinutes);

            CronExpression expression = CronExpression.Parse(Cron);
            var occurences = expression.GetOccurrences(bufferStart, expires);

            foreach (DateTime d in occurences)
            {
                if (d < pointInTime && d < expires)
                {
                    return true;
                }
            }

            return false;
        }

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            throw new NotImplementedException();
        }
    }
  
}
