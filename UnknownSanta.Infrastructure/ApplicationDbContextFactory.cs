using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UnknownSanta.Infrastructure;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    // Метод для создания контекста базы данных во время разработки
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Создание объекта для конфигурации контекста базы данных
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Указание строки подключения к базе данных SQLite
        // Строка подключения указывает путь к файлу базы данных SantaDB.db
        optionsBuilder.UseSqlite("Data Source=D:/UnknownSanta/SantaDB.db");
        
        // Возвращает новый экземпляр контекста с настроенными параметрами
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}