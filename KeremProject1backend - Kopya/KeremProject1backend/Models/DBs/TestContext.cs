using Microsoft.EntityFrameworkCore;


namespace KeremProject1backend.Models.DBs
{
    public class TestContext : DbContext
    {
        private static string? _connectionstring;

        public TestContext()
        {
        }

        public TestContext(DbContextOptions<TestContext> options) : base(options)
        {
        }


        public virtual DbSet<Test> Test { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("Test");

            modelBuilder.Entity<Test>(entity =>
            {
                entity.HasKey(e => new { e.Type, e.Id }).HasName("PK_Test");
            });
        }
    }
}