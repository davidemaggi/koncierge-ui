using Koncierge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Environment;

namespace Koncierge.Data
{
    public class KonciergeDbContext : DbContext
    {

        // remove-migration -StartUpProject Koncierge.Data
        // add-migration <Nome> -StartUpProject Koncierge.Data -o Migrations
        // add-migration InitialMigration -StartUpProject Koncierge.Data -o Migrations


        public DbSet<KonciergeKubeConfig> KubeConfigs { get; set; }
        public DbSet<KonciergeForwardContext> ForwardContexts { get; set; }
        public DbSet<KonciergeForwardNamespace> ForwardNameSpaces { get; set; }
        public DbSet<KonciergeForward> Forwards { get; set; }
        public DbSet<KonciergeForwardAdditionalConfig> ForwardLinkedConfigs { get; set; }
        public DbSet<KonciergeForwardAdditionalConfigItem> ForwardLinkedConfigItems { get; set; }

        

        public KonciergeDbContext() : base()
        {

          // Initialize();

        }
        public KonciergeDbContext(DbContextOptions<KonciergeDbContext> options) : base(options)
        {
            //Initialize();
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string AppPath = Path.Combine(GetFolderPath(SpecialFolder.LocalApplicationData, SpecialFolderOption.DoNotVerify), "koncierge");

            Directory.CreateDirectory(AppPath);

            VerifyDirectoryPermissions(AppPath);


            optionsBuilder.UseSqlite($"Data Source={Path.Combine(AppPath, "koncierge.db")};");



        }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {




        }

        private void VerifyDirectoryPermissions(string path)
        {
            try
            {
                var testFile = Path.Combine(path, "write_test.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                throw new Exception($"No write permissions to {path}. Error: {ex.Message}");
            }
        }
    }
}
