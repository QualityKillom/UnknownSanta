            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;
            using Telegram.Bot;
            using Telegram.Bot.Polling;
            using UnknownSanta.Infrastructure;
            using UnknownSantaa.Application.Service;

            string botToken = "7511400525:AAEgYvaVMX2TiVqsEHcMTdvFeAFcHzKYWQ0";
            
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            
            IConfiguration configuration = builder.Build();
            
            var serviceProvider = new ServiceCollection()
                .AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken))
                .AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")))
                .AddSingleton<ServiceGenerator>() 
                .BuildServiceProvider();
            
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.EnsureCreated();
            }

            var botClient = serviceProvider.GetRequiredService<ITelegramBotClient>();
            var serviceGenerator = serviceProvider.GetRequiredService<ServiceGenerator>(); 
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, 
            };
            
            using var cts = new CancellationTokenSource();

            botClient.StartReceiving(
                updateHandler: (client, update, token) => serviceGenerator.HandleUpdateAsync(client, update, token),
                errorHandler: async (client, exception, token) =>
                {
                    Console.WriteLine($"Polling error: {exception.Message}");
                    await Task.CompletedTask;
                },
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );




            Console.WriteLine("Bot is running... Press any key to exit.");
            Console.ReadKey();

            cts.Cancel();