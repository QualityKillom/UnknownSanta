using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using UnknownSanta.Infrastructure;
using UnknownSantaa.Application.Service;


// Задаем токен для подключения к боту. Токен следует хранить в безопасном месте.
string botToken = "7511400525:AAEgYvaVMX2TiVqsEHcMTdvFeAFcHzKYWQ0";

// Настройка конфигурации для приложения
var builder = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)  // Устанавливаем путь к текущей директории
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);  // Добавляем файл конфигурации JSON

// Создаем объект конфигурации
IConfiguration configuration = builder.Build();

// Создаем и настраиваем DI контейнер
var serviceProvider = new ServiceCollection()
    .AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken))  // Регистрация клиента бота
    .AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(configuration.GetConnectionString("DefaultConnection")))  // Настройка подключения к базе данных SQLite из строки подключения в конфиге
    .AddSingleton<ServiceGenerator>()  // Регистрация сервиса генератора, который будет обрабатывать обновления от Telegram
    .BuildServiceProvider();  // Строим DI контейнер

// Создаем область (scope) для работы с базой данных
using (var scope = serviceProvider.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();  // Получаем контекст базы данных
    dbContext.Database.EnsureCreated();  // Обеспечиваем создание базы данных, если она не существует
}

// Получаем экземпляры клиента бота и сервиса из контейнера зависимостей
var botClient = serviceProvider.GetRequiredService<ITelegramBotClient>();
var botService = serviceProvider.GetRequiredService<ServiceGenerator>();

// Создаем токен для отмены задач, если необходимо остановить получение обновлений
using var cts = new CancellationTokenSource();

// Запускаем процесс получения обновлений от бота
botClient.StartReceiving(
    updateHandler: async (client, update, token) => 
    {
        // Обрабатываем входящие обновления
        if (update.CallbackQuery != null)
        {
            await botService.HandleCallbackQueryAsync(update.CallbackQuery);  // Обработка callback-запросов
        }
        else
        {
            await botService.HandleUpdateAsync(update);  // Обработка обычных обновлений
        }
    },
    errorHandler: async (client, exception, token) =>
    {
        // Логирование ошибок при получении обновлений
        Console.WriteLine($"Polling error: {exception.Message}");
        await Task.CompletedTask;
    },
    cancellationToken: cts.Token  // Устанавливаем токен отмены для возможности остановки получения обновлений
);

// Сообщение о запуске бота
Console.WriteLine("Bot is running... Press any key to exit.");
Console.ReadKey();  // Ожидаем нажатия клавиши для завершения работы

cts.Cancel();  // Отправляем команду на отмену получения обновлений при выходе

