using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;

namespace ShorterUrl.Core.Domain
{
    public class ShortUrlEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string Url { get; set; }
        private string _activeUrl { get; set; }
        public string ActiveUrl => _activeUrl ??= GetActiveUrl();
        public string Title { get; set; }
        public string ShortUrl { get; set; }
        public int Clicks { get; set; }
        public bool? IsArchived { get; set; }
        public string SchedulesPropertyRaw { get; set; }
        private List<ScheduleEntity> _schedules { get; set; }

        public List<ScheduleEntity> Schedules
        {
            get
            {
                if (_schedules == null)
                {
                    _schedules = string.IsNullOrEmpty(SchedulesPropertyRaw)
                        ? new List<ScheduleEntity>()
                        : JsonSerializer.Deserialize<List<ScheduleEntity>>(SchedulesPropertyRaw);
                }
                return _schedules;
            }
            set => _schedules = value;
        }

        public ShortUrlEntity() { }

        public ShortUrlEntity(string longUrl, string endUrl)
        {
            Initialize(longUrl, endUrl, string.Empty, null);
        }

        public ShortUrlEntity(string longUrl, string endUrl, ScheduleEntity[] schedules)
        {
            Initialize(longUrl, endUrl, string.Empty, schedules);
        }

        public ShortUrlEntity(string longUrl, string endUrl, string title, ScheduleEntity[] schedules)
        {
            Initialize(longUrl, endUrl, title, schedules);
        }

        private void Initialize(string longUrl, string endUrl, string title, ScheduleEntity[] schedules)
        {
            PartitionKey = endUrl.First().ToString();
            RowKey = endUrl;
            Url = longUrl;
            Title = title;
            Clicks = 0;
            IsArchived = false;

            if (schedules?.Length > 0)
            {
                Schedules = schedules.ToList();
                SchedulesPropertyRaw = JsonSerializer.Serialize(Schedules);
            }
        }

        public static ShortUrlEntity GetEntity(string longUrl, string endUrl, string title, ScheduleEntity[] schedules)
        {
            return new ShortUrlEntity
            {
                PartitionKey = endUrl.First().ToString(),
                RowKey = endUrl,
                Url = longUrl,
                Title = title,
                Schedules = schedules.ToList()
            };
        }

        private string GetActiveUrl()
        {
            return GetActiveUrl(DateTime.UtcNow);
        }

        private string GetActiveUrl(DateTime pointInTime)
        {
            var link = Url;
            var active = Schedules.Where(s =>
                s.End > pointInTime &&
                s.Start < pointInTime)
                .OrderBy(s => s.Start);

            foreach (var sched in active)
            {
                if (sched.IsActive(pointInTime))
                {
                    link = sched.AlternativeUrl;
                    break;
                }
            }
            return link;
        }

    }

   
}
