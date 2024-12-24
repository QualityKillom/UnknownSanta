using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UnknownSanta.Infrastructure;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        // Исправлена строка подключения
        optionsBuilder.UseSqlite("Data Source=D:/UnknownSanta/SantaDB.db");
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}