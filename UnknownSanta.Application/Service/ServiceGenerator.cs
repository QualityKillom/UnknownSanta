using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UnknownSanta.Domain.Entities;
using UnknownSanta.Domain.ValueObjects.Enums;
using UnknownSanta.Infrastructure;

namespace UnknownSantaa.Application.Service;

public class ServiceGenerator
{
    // Поля для хранения клиента Telegram и контекста базы данных
    private readonly ITelegramBotClient _botClient; 
    private readonly ApplicationDbContext _dbContext;

    // Конструктор, который инициализирует поля и вызывает установку команд бота
    public ServiceGenerator(ITelegramBotClient botClient, ApplicationDbContext dbContext)
    {
        _botClient = botClient;   // Присваиваем переданный параметр botClient полю _botClient
        _dbContext = dbContext;   // Присваиваем переданный параметр dbContext полю _dbContext
        SetBotCommands().Wait();  // Вызываем асинхронный метод SetBotCommands() и ждем его завершения синхронно с помощью .Wait()
    }

   public async Task HandleUpdateAsync(Update update)
{
    try
    {
        // Проверка, что обновление является сообщением и текст сообщения не null
        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            // Извлечение данных о чате и пользователе
            var gameId = update.Message.Chat.Id;  // ID чата
            var chatType = update.Message.Chat.Type;  // Тип чата (Private, Group, Supergroup)
            var userId = update.Message.From.Id;  // ID пользователя
            var username = update.Message.From.Username ?? "пользователь";  // Имя пользователя или "пользователь", если оно не задано
            var fullname = (update.Message.From.FirstName + " " + update.Message.From.LastName)?.Trim() ?? "Иван Иваныч";  // Полное имя или "Иван Иваныч"

            // Получение команды из текста сообщения
            var messageText = update.Message.Text.Split(' ')[0].ToLower();  // Преобразуем команду в нижний регистр

            // Если сообщение не является командой (не начинается с "/"), выходим
            if (!messageText.StartsWith("/"))
                return;

            // Удаляем часть после символа @ из команды, если она есть
            messageText = messageText.Contains('@') ? messageText.Split('@')[0] : messageText;

            // Обработка команды в зависимости от типа чата
            if (chatType == ChatType.Private)
            {
                // Если чат приватный, обрабатываем команду для пользователя
                await HandlePrivateCommand(messageText, gameId, userId, username, fullname);
            }
            else if (chatType == ChatType.Group || chatType == ChatType.Supergroup)
            {
                // Если чат групповой, обрабатываем команду для группы
                await HandleGroupCommand(messageText, gameId, chatType, userId, username, update.Message);
            }
            else
            {
                // Для других типов чатов (например, каналов) отправляем сообщение об ошибке
                await _botClient.SendTextMessageAsync(gameId, "⚠️ Этот бот не поддерживает данный тип чатов.");
            }
        }
    }
    catch (Exception ex)
    {
        // Ловим ошибки и выводим их в консоль
        Console.WriteLine($"Ошибка в обработке обновления: {ex}");

        // Отправляем сообщение о возникшей ошибке в чат
        if (update.Message?.Chat != null)
        {
            await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "❌ Произошла ошибка. Попробуйте позже.");
        }
    }
}


    private async Task HandlePrivateCommand(string command, long chatId, long userId, string username, string fullname)
{
    switch (command)
    {
        // Команда /start, приветствие пользователя и краткое объяснение роли бота
        case "/start":
            await _botClient.SendTextMessageAsync(chatId, $"\ud83c\udf85 Привет, {fullname}! Я — бот Тайный Санта! \ud83c\udf81\n\nМеня зовут Санта, и я здесь, чтобы помочь вам организовать увлекательную игру \"Тайный Санта\". \ud83d\ude04 Мы будем дарить друг другу подарки, добавлять веселья и теплоты в ваши праздники. \ud83c\udf84");
            break;

        // Команда /info, предоставление подробной информации о возможностях бота
        case "/info":
            await _botClient.SendTextMessageAsync(chatId, "Что я умею?\n\n— Создавать группы участников для игры \"Тайный Санта\".\n— Автоматически распределять, кто кому дарит подарок. \ud83e\udd2b (Это секрет, конечно же!)\n— Напоминать правила игры.\n— Добавлять немного веселья через мини-игры, как, например, снежки! \u2744\ufe0f\n\nКак начать?\n\nВ личных сообщениях вы можете узнать обо мне больше, используя команду /info.\nВ групповых чатах я могу организовать игру: просто добавьте меня и используйте команды.\n\nСписок команд:\n\n/start — начать новую игру.\n/join — присоединиться к игре.\n/info — узнать больше о текущей игре.\n/stop — завершить игру.\n/restart — перезапустить игру.\n/snowball — устроить снежную битву!");
            break;

        // Обработка неизвестных команд
        default:
            await _botClient.SendTextMessageAsync(chatId, "❓ Неизвестная команда. Попробуйте /start или /info.");
            break;
    }
}


    private async Task HandleGroupCommand(string command, long chatId, ChatType chatType, long userId, string username, Message message)
{
    switch (command)
    {
        // Команда /start: начало игры с параметрами (валюта, сумма)
        case "/start":
            string[] args = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args.Length >= 3 && decimal.TryParse(args[2], out var amount))
            {
                string currency = args[1];
                // Вызов метода для обработки начала игры
                await HandleStartCommand(chatId, chatType.ToString().ToLower(), userId, currency, amount);
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId,
                    "⚠️ Используйте команду в формате: /start <валюта> <сумма>\nПример: /start USD 20");
            }
            break;

        // Команда /stop: завершение игры
        case "/stop":
            await HandleStopCommand(chatId, chatType.ToString().ToLower(), userId);
            break;

        // Команда /reset: сброс игры
        case "/reset":
            await HandleResetCommand(chatId, chatType.ToString().ToLower(), userId);
            break;

        // Команда /join: присоединение к игре
        case "/join":
            await HandleJoinCommand(chatId, userId, username);
            break;

        // Команда /info: просмотр всех участников игры
        case "/info":
            await ShowAllParticipants(chatId, chatType.ToString().ToLower());
            break;

        // Обработка неизвестных команд
        default:
            await _botClient.SendTextMessageAsync(chatId,
                "❓ Неизвестная команда. Попробуйте /start, /stop, /reset или /join.");
            break;
    }
}

    public async Task HandleStartCommand(long gameId, string chatType, long userId, string currency, decimal amount)
{
    // Проверка, является ли пользователь администратором и может ли он начать игру
    if (!await ValidateGroupAndAdmin(gameId, chatType, userId))
        return;

    // Получение информации о текущей игре
    var game = await _dbContext.Games.FirstOrDefaultAsync(c => c.Game_Id == gameId);

    // Формирование текста с условиями игры (сумма и валюта)
    string gameConditions = $"\ud83d\udcb0 Сумма подарка: {amount} {currency}\n\n";

    if (game == null)
    {
        // Если игра не существует, создаем новую запись в базе данных
        _dbContext.Games.Add(new Games
        {
            Game_Id = gameId,
            ChatType = chatType,
            GameState = GameState.Registration,
            Currency = currency,
            Amount = amount
        });
        await _dbContext.SaveChangesAsync();

        // Создание inline-кнопки для участия в игре
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Участвовать", "join_game") }
        });

        // Отправка сообщения в чат с инструкциями для начала игры
        await _botClient.SendTextMessageAsync(
            gameId,
            $"✅ Чат зарегистрирован!\n\n🎁 Началась регистрация участников!\n" +
            gameConditions +
            $"Для участия в игре нажмите кнопку ниже.",
            replyMarkup: inlineKeyboard);
    }
    else
    {
        // Обработка состояний игры, если она уже существует
        switch (game.GameState)
        {
            // Игра завершена, перезапускаем регистрацию
            case GameState.Completed:
                game.GameState = GameState.Registration;
                game.Currency = currency;
                game.Amount = amount;
                _dbContext.Update(game);
                await _dbContext.SaveChangesAsync();

                await _botClient.SendTextMessageAsync(
                    gameId,
                    $"✅ Игра завершена. Начинаем новую регистрацию участников!\n" +
                    gameConditions +
                    $"Нажмите кнопку ниже для участия в игре.");
                break;

            // Игра в стадии регистрации, обновляем параметры и показываем участников
            case GameState.Registration:
                if (game.Currency != currency || game.Amount != amount)
                {
                    game.Currency = currency;
                    game.Amount = amount;
                    _dbContext.Update(game);
                    await _dbContext.SaveChangesAsync();
                }

                // Получаем список зарегистрированных участников
                var participants = await _dbContext.Users
                    .Where(p => p.Game_Id == gameId)
                    .ToListAsync();

                // Формируем строку с участниками
                var participantList = participants.Any()
                    ? "👥 Список зарегистрированных пользователей:\n" +
                      string.Join("\n", participants.Select(p => $"@{p.TagUserName} "))
                    : "❌ Пока нет зарегистрированных пользователей.";

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Участвовать", "join_game") }
                });

                // Отправка сообщения с участниками и возможностью присоединиться
                await _botClient.SendTextMessageAsync(
                    gameId,
                    $"{participantList}\n\n{gameConditions}" +
                    $"Нажмите кнопку ниже, чтобы участвовать.",
                    replyMarkup: inlineKeyboard);
                break;

            // Игра уже началась
            case GameState.InProgress:
                await _botClient.SendTextMessageAsync(gameId, "⚠️ Игра уже началась.");
                break;

            // Игра уже зарегистрирована
            default:
                await _botClient.SendTextMessageAsync(gameId, "⚠️ Чат уже зарегистрирован.");
                break;
        }
    }
}


    private async Task HandleJoinCommand(long gameId, long userId, string username)
{
    // Пытаемся получить игру по gameId
    var game = await _dbContext.Games.FirstOrDefaultAsync(c => c.Game_Id == gameId);
    
    // Если игра не найдена, отправляем сообщение, что чат не зарегистрирован
    if (game == null)
    {
        await _botClient.SendTextMessageAsync(gameId,
            "⚠️ Чат не зарегистрирован. Сначала используйте команду /start.");
        return;
    }

    // Проверка, что игра находится в стадии регистрации
    if (game.GameState != GameState.Registration)
    {
        // В зависимости от состояния игры отправляем разные сообщения
        string message = game.GameState switch
        {
            GameState.Completed => "⚠️ Регистрация завершена. Сначала используйте команду /start для новой игры.",
            GameState.InProgress => "⚠️ Игра уже началась. Регистрация участников невозможна.",
            _ => "⚠️ Чат неактивен. Используйте команду /start для начала регистрации."
        };
        await _botClient.SendTextMessageAsync(gameId, message);
        return;
    }

    // Проверяем, зарегистрирован ли уже пользователь
    var participantExists = await _dbContext.Users
        .AnyAsync(p => p.Telegram_Id == userId && p.Game_Id == gameId);

    if (!participantExists)
    {
        // Если пользователь не зарегистрирован, добавляем его в базу данных
        _dbContext.Users.Add(new Users
        {
            Telegram_Id = userId,
            TagUserName = username,
            Game_Id = gameId,
        });
        await _dbContext.SaveChangesAsync();

        // Получаем список всех участников игры
        var participants = await _dbContext.Users
            .Where(p => p.Game_Id == gameId)
            .ToListAsync();

        // Формируем список участников
        var participantList = participants.Any()
            ? string.Join("\n", participants.Select(p => $"@{p.TagUserName}"))
            : "❌ Пока нет участников.";

        // Формируем текст с условиями игры
        string gameConditions = $"🎁 Условия игры:\n💰 Сумма подарка: {game.Amount} {game.Currency}\n";

        // Создаем inline-кнопку для участия
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Участвовать", "join_game") }
        });

        // Отправляем сообщение о регистрации с условиями игры и списком участников
        await _botClient.SendTextMessageAsync(gameId,
            $"✅ @{username}, вы зарегистрированы!\n\n" +
            gameConditions + 
            $"👥 Список участников:\n{participantList}\n\n" +
            "Для участия в игре нажмите кнопку ниже.",
            replyMarkup: inlineKeyboard);

        // Если пользователь еще не получал сообщения от бота, отправляем напоминание
        if (!await HasUserReceivedMessageAsync(userId))
        {
            await _botClient.SendTextMessageAsync(gameId,
                $"⚠️ @{username}, пожалуйста, напишите боту в личных сообщениях, чтобы он смог отправлять вам уведомления!");
        }
    }
    else
    {
        // Если пользователь уже зарегистрирован, отправляем соответствующее сообщение
        await _botClient.SendTextMessageAsync(gameId, $"⚠️ @{username}, вы уже зарегистрированы.");
    }
}

    private async Task HandleStopCommand(long gameId, string chatType, long userId)
    {
        // Проверка, что пользователь является администратором чата и что группа активна
        if (!await ValidateGroupAndAdmin(gameId, chatType, userId))
            return;

        // Получаем игру по gameId, включая всех участников
        var game = await _dbContext.Games
            .Include(c => c.Users)
            .FirstOrDefaultAsync(c => c.Game_Id == gameId);

        // Если игра не найдена или нет участников, отправляем сообщение
        if (game == null || !game.Users.Any())
        {
            await _botClient.SendTextMessageAsync(gameId, "⚠️ Нет участников для завершения регистрации.");
            return;
        }

        // Проверяем состояние игры, чтобы убедиться, что можно завершить регистрацию
        if (game.GameState != GameState.Registration)
        {
            string message = game.GameState switch
            {
                GameState.InProgress => "⚠️ Игра уже началась. Вы не можете остановить регистрацию.",
                GameState.Completed => "⚠️ Игра уже завершена. Для начала новой игры используйте /start.",
                _ => "⚠️ Чат неактивен или команда недоступна в текущем состоянии."
            };
            await _botClient.SendTextMessageAsync(gameId, message);
            return;
        }

        // Пытаемся распределить пары. Если не удалось — завершаем команду
        bool isDistributionSuccessful = await DistributePairs(gameId);

        if (!isDistributionSuccessful)
        {
            return;
        }

        // Обновляем состояние игры на завершённое и удаляем пользователей из базы
        game.GameState = GameState.Completed;
        _dbContext.Users.RemoveRange(game.Users);
        await _dbContext.SaveChangesAsync();

        // Отправляем сообщение о завершении регистрации и игры
        await _botClient.SendTextMessageAsync(gameId, "🛑 Регистрация завершена. Пары распределены, и игра завершена.");
    }


    private async Task HandleResetCommand(long gameId, string chatType, long userId)
    {
        // Проверка, что пользователь является администратором чата и что группа активна
        if (!await ValidateGroupAndAdmin(gameId, chatType, userId))
            return;

        // Получаем игру по gameId, включая всех участников
        var game = await _dbContext.Games
            .Include(c => c.Users)
            .FirstOrDefaultAsync(c => c.Game_Id == gameId);

        // Если игра найдена, удаляем все данные (пользователей и игру)
        if (game != null)
        {
            _dbContext.Users.RemoveRange(game.Users);  // Удаление всех участников игры
            _dbContext.Games.Remove(game);  // Удаление игры
            await _dbContext.SaveChangesAsync();  // Сохранение изменений в базе данных

            // Отправляем сообщение о сбросе игры
            await _botClient.SendTextMessageAsync(gameId, "🔄 Игра сброшена. Чат и участники удалены.");
        }
        else
        {
            // Если игра не найдена, отправляем сообщение о том, что чат не зарегистрирован
            await _botClient.SendTextMessageAsync(gameId, "⚠️ Чат не зарегистрирован.");
        }
    }


    public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
{
    var chatId = callbackQuery.Message.Chat.Id;
    var userId = callbackQuery.From.Id;

    try
    {
        // Обработка нажатия кнопки "join_game"
        if (callbackQuery.Data == "join_game")
        {
            var game = await _dbContext.Games.FirstOrDefaultAsync(c => c.Game_Id == chatId);
            
            // Проверка, существует ли игра и находится ли она в состоянии регистрации
            if (game == null || game.GameState != GameState.Registration)
            {
                await _botClient.SendTextMessageAsync(chatId, "⚠️ Регистрация не доступна в данный момент.");
                return;
            }

            // Проверка, зарегистрирован ли пользователь
            var participantExists = await _dbContext.Users
                .AnyAsync(p => p.Telegram_Id == userId && p.Game_Id == chatId);

            if (!participantExists)
            {
                // Добавляем пользователя в игру
                _dbContext.Users.Add(new Users
                {
                    Telegram_Id = userId,
                    Game_Id = chatId,
                    TagUserName = callbackQuery.From.Username,
                });
                await _dbContext.SaveChangesAsync();

                // Получаем список участников
                var participants = await _dbContext.Users
                    .Where(p => p.Game_Id == chatId)
                    .ToListAsync();

                // Формируем строку с участниками
                var participantList = participants.Any() 
                    ? string.Join("\n", participants.Select(p => $"@{p.TagUserName} "))
                    : "❌ Пока нет участников.";

                string gameConditions = $"🎁 Условия игры:\n💰 Сумма подарка: {game.Amount} {game.Currency}\n";

                // Формируем клавиатуру для участия
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Участвовать", "join_game") }
                });

                // Редактируем сообщение
                await _botClient.EditMessageTextAsync(
                    chatId,
                    callbackQuery.Message.MessageId,
                    $"✅ @{callbackQuery.From.Username}, вы зарегистрированы!\n\n" +
                    gameConditions + 
                    $"👥 Список участников:\n{participantList}\n\n" +
                    "Для участия в игре нажмите кнопку ниже.",
                    replyMarkup: inlineKeyboard);
            }
            else
            {
                // Если пользователь уже зарегистрирован
                await _botClient.SendTextMessageAsync(chatId, $"⚠️ @{callbackQuery.From.Username}, вы уже зарегистрированы.");
            }
        }
    }
    catch (Exception ex)
    {
        // Логирование ошибок и отправка сообщения пользователю
        Console.WriteLine($"Ошибка при обработке запроса: {ex.Message}");
        await _botClient.SendTextMessageAsync(chatId, "Произошла ошибка при обработке вашего запроса. Попробуйте снова.");
    }
}


    private async Task<bool> HasUserReceivedMessageAsync(long userId)
    {
        try
        {
            return await _dbContext.SendMessages.AnyAsync(sm => sm.Telegram_Id == userId);
        }
        catch (Exception ex)
        {
            // Логируем ошибку
            Console.WriteLine($"Ошибка при проверке наличия сообщения для пользователя {userId}: {ex.Message}");
            return false;  // Возвращаем false в случае ошибки
        }
    }


    private async Task<bool> DistributePairs(long chatId)
{
    // Получаем список пользователей, зарегистрированных в игре с указанным chatId
    var users = _dbContext.Users.Where(p => p.Game_Id == chatId).ToList();

    // Если участников меньше двух, сообщаем об этом и прекращаем выполнение
    if (users.Count < 2)
    {
        await _botClient.SendTextMessageAsync(chatId, "❗ Недостаточно участников для распределения пар.");
        return false;
    }

    // Списки для хранения проблемных и валидных пользователей
    var problematicUsers = new List<string>();
    var validUsers = new List<Users>();

    // Проходим по всем пользователям и проверяем, можем ли мы с ними связаться
    foreach (var user in users)
    {
        if (await CanCommunicateWithUser(user.Telegram_Id))
        {
            // Если можем связаться, добавляем пользователя в список валидных
            validUsers.Add(user);
        }
        else
        {
            // Если не можем связаться, добавляем пользователя в список проблемных
            problematicUsers.Add($"@{user.TagUserName}");
        }
    }

    // Если есть проблемы с участниками, сообщаем об этом
    if (problematicUsers.Any())
    {
        await _botClient.SendTextMessageAsync(
            chatId,
            $"⚠️ Не удалось связаться со следующими участниками: {string.Join(", ", problematicUsers)}. Попросите их написать команду /start боту в личных сообщениях."
        );
    }

    // Если валидных пользователей меньше двух, сообщаем об этом и прекращаем выполнение
    if (validUsers.Count < 2)
    {
        await _botClient.SendTextMessageAsync(chatId, "❗ Недостаточно доступных участников для распределения пар.");
        return false;
    }

    // Перемешиваем список валидных пользователей случайным образом
    var shuffled = validUsers.OrderBy(_ => Guid.NewGuid()).ToList();

    // Проходим по перемешанному списку и распределяем пары
    for (int i = 0; i < shuffled.Count; i++)
    {
        var giver = shuffled[i];  // Текущий пользователь, который будет дарить подарок
        var receiver = shuffled[(i + 1) % shuffled.Count];  // Следующий пользователь (или первый, если мы на последнем)

        try
        {
            // Отправляем сообщение пользователю о том, кто его получатель
            await SendMessageAndLogAsync(giver.Telegram_Id, $"🎁 Вы должны подарить подарок @{receiver.TagUserName}!");
        }
        catch (Exception ex)
        {
            // Если ошибка при отправке сообщения, выводим ошибку в консоль и уведомляем о проблеме
            Console.WriteLine($"Ошибка при отправке сообщения @{giver.TagUserName}: {ex.Message}");
            await _botClient.SendTextMessageAsync(
                chatId,
                $"⚠️ Ошибка при уведомлении @{giver.TagUserName}. Возможно, бот заблокирован."
            );
        }
    }

    // Когда все пары распределены и уведомления отправлены, отправляем сообщение об успешном завершении
    await _botClient.SendTextMessageAsync(chatId, "✅ Все пары распределены и уведомлены в личных сообщениях!");
    return true;
}


    private async Task<bool> CanCommunicateWithUser(long userId)
    {
        // Проверяем, существует ли запись о сообщении, отправленном пользователю, в базе данных.
        bool messageExists = _dbContext.SendMessages.Any(x => x.Telegram_Id == userId);

        // Если сообщения пользователю не было отправлено ранее, проводим проверку доступности
        if (!messageExists)
        {
            try
            {
                // Пытаемся отправить тестовое сообщение пользователю
                await _botClient.SendTextMessageAsync(userId, "Проверка доступности бота.");
                return true; // Если сообщение отправлено успешно, возвращаем true
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                // Если возникло исключение, проверяем, заблокировал ли пользователь бота
                if (ex.Message.Contains("Forbidden") || ex.Message.Contains("bot was blocked by the user"))
                {
                    Console.WriteLine($"Бот заблокирован пользователем: {userId}");
                    return false; // Возвращаем false, если пользователь заблокировал бота
                }

                // Если ошибка другого типа, пробрасываем исключение дальше
                throw;
            }
            catch
            {
                // Если произошла другая ошибка, возвращаем false
                return false;
            }
        }
        else
        {
            // Если сообщение ранее отправлялось и существует запись в базе, считаем пользователя доступным
            return true;
        }
    }


    private async Task SendMessageAndLogAsync(long userId, string message)
    {
        try
        {
            // Пытаемся отправить текстовое сообщение пользователю
            await _botClient.SendTextMessageAsync(userId, message);

            // Проверяем, есть ли запись о сообщении, отправленном этому пользователю в базе данных
            bool messageExists = _dbContext.SendMessages.Any(x => x.Telegram_Id == userId);

            // Если записи о сообщении нет, добавляем новую запись в базу данных
            if (!messageExists)
            {
                _dbContext.SendMessages.Add(new SendMessage
                {
                    Telegram_Id = userId,
                    SentAt = DateTime.UtcNow // Сохраняем время отправки в UTC
                });
            }

            // Сохраняем изменения в базе данных
            await _dbContext.SaveChangesAsync();
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex)
        {
            // Обрабатываем ошибки, связанные с отправкой сообщений
            if (ex.Message.Contains("Forbidden") || ex.Message.Contains("bot was blocked by the user"))
            {
                // Если пользователь заблокировал бота, выводим сообщение в консоль
                Console.WriteLine($"Пользователь заблокировал бота: {userId}");
            }
            else
            {
                // В других случаях выводим сообщение об ошибке отправки
                Console.WriteLine($"Ошибка отправки сообщения пользователю {userId}: {ex.Message}");
            }
        }
    }


    private async Task SetBotCommands()
    {
        // Список команд для групповых чатов
        var groupCommands = new List<BotCommand>
        {
            new BotCommand { Command = "start", Description = "Начать новую игру" },
            new BotCommand { Command = "stop", Description = "Завершить регистрацию и распределить пары" },
            new BotCommand { Command = "reset", Description = "Сбросить текущую игру" },
            new BotCommand { Command = "join", Description = "Присоединиться к игре" },
            new BotCommand { Command = "info", Description = "Информация об участниках" },
        };

        // Список команд для личных сообщений
        var privateCommands = new List<BotCommand>
        {
            new BotCommand { Command = "start", Description = "Запустить бота" },
            new BotCommand { Command = "info", Description = "Информация о боте" },
        };

        // Устанавливаем команды для всех групповых чатов
        await _botClient.SetMyCommandsAsync(groupCommands, new BotCommandScopeAllGroupChats());
    
        // Устанавливаем команды для всех личных чатов
        await _botClient.SetMyCommandsAsync(privateCommands, new BotCommandScopeAllPrivateChats());
    }


    private async Task<bool> ValidateGroupAndAdmin(long gameId, string chatType, long userId, string errorMessage = null)
    {
        // Проверка, что чат является групповым или супергрупповым
        if (chatType != "group" && chatType != "supergroup")
        {
            await _botClient.SendTextMessageAsync(gameId, "⚠️ Эта команда доступна только в групповых чатах.");
            return false;
        }

        // Проверка, что пользователь является администратором
        if (!await IsUserAdmin(gameId, userId))
        {
            await _botClient.SendTextMessageAsync(gameId,
                errorMessage ?? "❌ У вас нет прав администратора для выполнения этой команды.");
            return false;
        }

        // Если все проверки пройдены, возвращаем true
        return true;
    }


    private async Task<bool> IsUserAdmin(long gameId, long userId)
    {
        try
        {
            // Получаем список администраторов чата с gameId
            var admins = await _botClient.GetChatAdministratorsAsync(gameId);
        
            // Проверяем, есть ли в списке администраторов пользователь с указанным userId
            return admins.Any(a => a.User.Id == userId);
        }
        catch
        {
            // В случае ошибки (например, если не удается получить администраторов) возвращаем false
            return false;
        }
    }


    private async Task ShowAllParticipants(long gameId, string chatType)
    {
        // Проверяем, что команда вызвана в групповом чате (группе или супер-группе)
        if (chatType != "group" && chatType != "supergroup")
        {
            // Отправляем сообщение, если команда используется не в групповом чате
            await _botClient.SendTextMessageAsync(gameId, "⚠️ Эта команда доступна только в групповых чатах.");
            return;
        }

        // Получаем список участников игры из базы данных по gameId
        var users = await _dbContext.Users
            .Where(p => p.Game_Id == gameId)
            .ToListAsync();

        // Проверяем, есть ли зарегистрированные участники
        if (!users.Any())
        {
            // Отправляем сообщение, если нет участников
            await _botClient.SendTextMessageAsync(gameId, "❌ Участники не найдены.");
            return;
        }

        // Формируем строку со списком участников, форматируя каждый username в виде @username
        var participantList = string.Join("\n", users.Select(p => $"@{p.TagUserName}"));
    
        // Отправляем сообщение с участниками
        await _botClient.SendTextMessageAsync(gameId, $"👥 Список участников:\n{participantList}");
    }

}
