using Microsoft.EntityFrameworkCore;

namespace AuthServer.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        {
            
        }
    }
}
