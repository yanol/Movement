using Microsoft.EntityFrameworkCore;

namespace DataService
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<DataItem> DataItems => Set<DataItem>();
    }
}
