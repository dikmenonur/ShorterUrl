﻿using Microsoft.EntityFrameworkCore;
using ShorterUrl.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShorterUrl.Core.Datasource
{
    public class UrlShortenerContext : DbContext, IUrlShortenerContext
    {
        public UrlShortenerContext(DbContextOptions<UrlShortenerContext> options) : base(options)
        {
        }
        public DbSet<ShortUrlEntity> ShortUrlEntities { get; set; }
        public DbSet<ScheduleEntity> ScheduleEntities { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShortUrlEntity>()
                   .HasKey(c => new { c.PartitionKey, c.RowKey });

            modelBuilder.Entity<ShortUrlEntity>()
                .Property(e => e.SchedulesPropertyRaw);

            modelBuilder.Entity<ScheduleEntity>()
                .HasKey(c => new { c.PartitionKey, c.RowKey });
        }
    }
}
