using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlowLinkML
{
    using Models;

    public class AppContext : DbContext
    {
        
        //public DbSet<Device> Devices { get; set; }

        public DbSet<Archive> FlowmeterHourlyArchive { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("");
        }
    }
}
