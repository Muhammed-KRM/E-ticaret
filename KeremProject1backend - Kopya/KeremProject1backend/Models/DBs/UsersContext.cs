using KeremProject1backend.Services;
using Microsoft.EntityFrameworkCore;

namespace KeremProject1backend.Models.DBs
{
    public class UsersContext : DbContext
    {
        public UsersContext(DbContextOptions<UsersContext> options) : base(options)
        {
        }

        public virtual DbSet<AllUsersModels> AllUsersModel { get; set; }
        public virtual DbSet<DataLog> DataLog { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("Users");

            modelBuilder.Entity<AllUsersModels>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK_Users");

                entity.Property(e => e.Id)
                      .ValueGeneratedOnAdd()
                      .HasColumnName("Id");
            });

            modelBuilder.Entity<DataLog>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK_Users_Data_Log");

                entity.Property(e => e.Id)
                      .ValueGeneratedOnAdd()
                      .HasColumnName("Id");
            });
        }
    }
}
