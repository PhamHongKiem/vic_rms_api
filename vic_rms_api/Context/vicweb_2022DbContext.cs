using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using vic_rms_api.Models;
//using Pomelo.EntityFrameworkCore.MySql;

namespace vic_rms_api.Context
{
    public class vicweb_2022DbContext: DbContext
    {
        private readonly IConfiguration _configuration;
        public vicweb_2022DbContext(DbContextOptions<vicweb_2022DbContext> options) : base(options) { }
        public vicweb_2022DbContext(DbContextOptions<vicweb_2022DbContext> options, IConfiguration configuration)
        : base(options)
        {
            _configuration = configuration;
        }
        //public DbSet<Client> Client { get; set; }
        public DbSet<wp_rates> Wp_Rates { get; set; }
        public DbSet<wp_rates_grid> wp_rates_grid { get; set; }
        public DbSet<wp_hotels> wp_hotels { get; set; }
        //public DbSet<Category> Categories { get; set; }
        //public DbSet<daybreakdown> daybreakdown { get; set; }
        //public DbSet<Room> Rooms { get; set; }
        //public DbSet<Country> Countries { get; set; }
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<Additional>().HasKey(a => a.AdditionalId);
        //}


        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    if (!optionsBuilder.IsConfigured)
        //    {
        //        var connectionString = _configuration.GetValue<string>("ConnectionStrings:DefaultConnection"); ;
        //        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)),
        //            mySqlOptions => mySqlOptions.CommandTimeout(3000)); // Tăng thời gian chờ lệnh lên 120 giây
        //    }
        //}
    }
}
