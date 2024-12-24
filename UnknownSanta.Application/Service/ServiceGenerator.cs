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
     private readonly ITelegramBotClient _botClient;
    private readonly ApplicationDbContext _dbContext;

    public ServiceGenerator(ITelegramBotClient botClient, ApplicationDbContext dbContext)
    {
        _botClient = botClient;
        _dbContext = dbContext;
        SetBotCommands().Wait();
    }

    public async Task HandleUpdateAsync(Update update)
    {
        try
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                var gameId = update.Message.Chat.Id;
                var chatType = update.Message.Chat.Type;
                var userId = update.Message.From.Id;
                var username = update.Message.From.Username ?? "пользователь";
                var fullname = (update.Message.From.FirstName + " " + update.Message.From.LastName)?.Trim() ?? "Дим Димыч";

                var messageText = update.Message.Text.Split(' ')[0].ToLower();

                if (!messageText.StartsWith("/"))
                    return;

                messageText = messageText.Contains('@') ? messageText.Split('@')[0] : messageText;

                if (chatType == ChatType.Private)
                {
                    await HandlePrivateCommand(messageText, gameId, userId, username, fullname);
                }
                else if (chatType == ChatType.Group || chatType == ChatType.Supergroup)
                {
                    await HandleGroupCommand(messageText, gameId, chatType, userId, username, update.Message);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(gameId, "⚠️ Этот бот не поддерживает данный тип чатов.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в обработке обновления: {ex}");
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
            case "/start":
                await _botClient.SendTextMessageAsync(chatId, $"\ud83c\udf85 Привет, {fullname}! Я — бот Тайный Санта! \ud83c\udf81\n\nМеня зовут Санта, и я здесь, чтобы помочь вам организовать увлекательную игру \"Тайный Санта\". \ud83d\ude04 Мы будем дарить друг другу подарки, добавлять веселья и теплоты в ваши праздники. \ud83c\udf84");
                break;

            case "/info":
                await _botClient.SendTextMessageAsync(chatId, "Что я умею?\n\n— Создавать группы участников для игры \"Тайный Санта\".\n— Автоматически распределять, кто кому дарит подарок. \ud83e\udd2b (Это секрет, конечно же!)\n— Напоминать правила игры.\n— Добавлять немного веселья через мини-игры, как, например, снежки! \u2744\ufe0f\n\nКак начать?\n\nВ личных сообщениях вы можете узнать обо мне больше, используя команду /info.\nВ групповых чатах я могу организовать игру: просто добавьте меня и используйте команды.\n\nСписок команд:\n\n/start — начать новую игру.\n/join — присоединиться к игре.\n/info — узнать больше о текущей игре.\n/stop — завершить игру.\n/restart — перезапустить игру.\n/snowball — устроить снежную битву!");
                break;

            default:
                await _botClient.SendTextMessageAsync(chatId, "❓ Неизвестная команда. Попробуйте /start или /info.");
                break;
        }
    }

    private async Task HandleGroupCommand(string command, long chatId, ChatType chatType, long userId, string username, Message message)
    {
        switch (command)
        {
            case "/start":
                string[] args = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (args.Length >= 3 && decimal.TryParse(args[2], out var amount))
                {
                    string currency = args[1];
                    await HandleStartCommand(chatId, chatType.ToString().ToLower(), userId, currency, amount);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId,
                        "⚠️ Используйте команду в формате: /start <валюта> <сумма>\nПример: /start USD 20");
                }
                break;

            case "/stop":
                await HandleStopCommand(chatId, chatType.ToString().ToLower(), userId);
                break;

            case "/reset":
                await HandleResetCommand(chatId, chatType.ToString().ToLower(), userId);
                break;

            case "/join":
                await HandleJoinCommand(chatId, userId, username);
                break;

            case "/info":
                await ShowAllParticipants(chatId, chatType.ToString().ToLower());
                break;

            default:
                await _botClient.SendTextMessageAsync(chatId,
                    "❓ Неизвестная команда. Попробуйте /start, /stop, /reset или /join.");
                break;
        }
    }

    public async Task HandleStartCommand(long gameId, string chatType, long userId, string currency, decimal amount)
{
    if (!await ValidateGroupAndAdmin(gameId, chatType, userId))
        return;

    var game = await _dbContext.Games.FirstOrDefaultAsync(c => c.Game_Id == gameId);

    string gameConditions = $"\ud83d\udcb0 Сумма подарка: {amount} {currency}\n\n";

    if (game == null)
    {
        _dbContext.Games.Add(new Games
        {
            Game_Id = gameId,
            ChatType = chatType,
            GameState = GameState.Registration,
            Currency = currency,
            Amount = amount
        });
        await _dbContext.SaveChangesAsync();

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Участвовать", "join_game") }
        });

        await _botClient.SendTextMessageAsync(
            gameId,
            $"✅ Чат зарегистрирован!\n\n🎁 Началась регистрация участников!\n" +
            gameConditions +
            $"Для участия в игре нажмите кнопку ниже.",
            replyMarkup: inlineKeyboard);
    }
    else
    {
        switch (game.GameState)
        {
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

            case GameState.Registration:
                if (game.Currency != currency || game.Amount != amount)
                {
                    game.Currency = currency;
                    game.Amount = amount;
                    _dbContext.Update(game);
                    await _dbContext.SaveChangesAsync();
                }

                var participants = await _dbContext.Users
                    .Where(p => p.Game_Id == gameId)
                    .ToListAsync();

                var participantList = participants.Any()
                    ? "👥 Список зарегистрированных пользователей:\n" +
                      string.Join("\n", participants.Select(p => $"@{p.TagUserName} "))
                    : "❌ Пока нет зарегистрированных пользователей.";

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Участвовать", "join_game") }
                });

                await _botClient.SendTextMessageAsync(
                    gameId,
                    $"{participantList}\n\n{gameConditions}" +
                    $"Нажмите кнопку ниже, чтобы участвовать.",
                    replyMarkup: inlineKeyboard);
                break;

            case GameState.InProgress:
                await _botClient.SendTextMessageAsync(gameId, "⚠️ Игра уже началась.");
                break;

            default:
                await _botClient.SendTextMessageAsync(gameId, "⚠️ Чат уже зарегистрирован.");
                break;
        }
    }
}

    private async Task HandleJoinCommand(long gameId, long userId, string username)
{
    var game = await _dbContext.Games.FirstOrDefaultAsync(c => c.Game_Id == gameId);
    if (game == null)
    {
        await _botClient.SendTextMessageAsync(gameId,
            "⚠️ Чат не зарегистрирован. Сначала используйте команду /start.");
        return;
    }

    if (game.GameState != GameState.Registration)
    {
        string message = game.GameState switch
        {
            GameState.Completed => "⚠️ Регистрация завершена. Сначала используйте команду /start для новой игры.",
            GameState.InProgress => "⚠️ Игра уже началась. Регистрация участников невозможна.",
            _ => "⚠️ Чат неактивен. Используйте команду /start для начала регистрации."
        };
        await _botClient.SendTextMessageAsync(gameId, message);
        return;
    }

    var participantExists = await _dbContext.Users
        .AnyAsync(p => p.Telegram_Id == userId && p.Game_Id == gameId);

    if (!participantExists)
    {
        _dbContext.Users.Add(new Users
        {
            Telegram_Id = userId,
            TagUserName = username,
            Game_Id = gameId,
        });
        await _dbContext.SaveChangesAsync();

        var participants = await _dbContext.Users
            .Where(p => p.Game_Id == gameId)
            .ToListAsync();

        var participantList = participants.Any()
            ? string.Join("\n", participants.Select(p => $"@{p.TagUserName}"))
            : "❌ Пока нет участников.";

        string gameConditions = $"🎁 Условия игры:\n💰 Сумма подарка: {game.Amount} {game.Currency}\n";

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Участвовать", "join_game") }
        });

        await _botClient.SendTextMessageAsync(gameId,
            $"✅ @{username}, вы зарегистрированы!\n\n" +
            gameConditions + 
            $"👥 Список участников:\n{participantList}\n\n" +
            "Для участия в игре нажмите кнопку ниже.",
            replyMarkup: inlineKeyboard);

        if (!await HasUserReceivedMessageAsync(userId))
        {
            await _botClient.SendTextMessageAsync(gameId,
                $"⚠️ @{username}, пожалуйста, напишите боту в личных сообщениях, чтобы он смог отправлять вам уведомления!");
        }
    }
    else
    {
        await _botClient.SendTextMessageAsync(gameId, $"⚠️ @{username}, вы уже зарегистрированы.");
    }
}

    private async Task HandleStopCommand(long gameId, string chatType, long userId)
    {
        if (!await ValidateGroupAndAdmin(gameId, chatType, userId))
            return;

        var game = await _dbContext.Games
            .Include(c => c.Users)
            .FirstOrDefaultAsync(c => c.Game_Id == gameId);

        if (game == null || !game.Users.Any())
        {
            await _botClient.SendTextMessageAsync(gameId, "⚠️ Нет участников для завершения регистрации.");
            return;
        }

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

        bool isDistributionSuccessful = await DistributePairs(gameId);

        if (!isDistributionSuccessful)
        {
            return;
        }

        game.GameState = GameState.Completed;
        _dbContext.Users.RemoveRange(game.Users);
        await _dbContext.SaveChangesAsync();

        await _botClient.SendTextMessageAsync(gameId, "🛑 Регистрация завершена. Пары распределены, и игра завершена.");
    }

    private async Task HandleResetCommand(long gameId, string chatType, long userId)
    {
        if (!await ValidateGroupAndAdmin(gameId, chatType, userId))
            return;

        var game = await _dbContext.Games
            .Include(c => c.Users)
            .FirstOrDefaultAsync(c => c.Game_Id == gameId);

        if (game != null)
        {
            _dbContext.Users.RemoveRange(game.Users);
            _dbContext.Games.Remove(game);
            await _dbContext.SaveChangesAsync();

            await _botClient.SendTextMessageAsync(gameId, "🔄 Игра сброшена. Чат и участники удалены.");
        }
        else
        {
            await _botClient.SendTextMessageAsync(gameId, "⚠️ Чат не зарегистрирован.");
        }
    }

    public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var userId = callbackQuery.From.Id;

        try
        {
            if (callbackQuery.Data == "join_game")
            {
                var game = await _dbContext.Games.FirstOrDefaultAsync(c => c.Game_Id == chatId);
                if (game == null || game.GameState != GameState.Registration)
                {
                    await _botClient.SendTextMessageAsync(chatId, "⚠️ Регистрация не доступна в данный момент.");
                    return;
                }

                var participantExists = await _dbContext.Users
                    .AnyAsync(p => p.Telegram_Id == userId && p.Game_Id == chatId);

                if (!participantExists)
                {
                    _dbContext.Users.Add(new Users
                    {
                        Telegram_Id = userId,
                        Game_Id = chatId,
                        TagUserName = callbackQuery.From.Username,
                    });
                    await _dbContext.SaveChangesAsync();

                    var participants = await _dbContext.Users
                        .Where(p => p.Game_Id == chatId)
                        .ToListAsync();

                    var participantList = string.Join("\n", participants.Select(p => $"@{p.TagUserName} "));
                    string gameConditions = $"🎁 Условия игры:\n💰 Сумма подарка: {game.Amount} {game.Currency}\n";

                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("Участвовать", "join_game") }
                    });

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
                    await _botClient.SendTextMessageAsync(chatId, $"⚠️ @{callbackQuery.From.Username}, вы уже зарегистрированы.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при редактировании сообщения: {ex.Message}");
            await _botClient.SendTextMessageAsync(chatId, "Произошла ошибка при обработке вашего запроса. Попробуйте снова.");
        }
    }

    private async Task<bool> HasUserReceivedMessageAsync(long userId)
    {
        return await _dbContext.SendMessages.AnyAsync(sm => sm.Telegram_Id == userId);
    }

    private async Task<bool> DistributePairs(long chatId)
    {
        var users = _dbContext.Users.Where(p => p.Game_Id == chatId).ToList();

        if (users.Count < 2)
        {
            await _botClient.SendTextMessageAsync(chatId, "❗ Недостаточно участников для распределения пар.");
            return false;
        }

        var problematicUsers = new List<string>();
        var validUsers = new List<Users>();

        foreach (var user in users)
        {
            if (await CanCommunicateWithUser(user.Telegram_Id))
            {
                validUsers.Add(user);
            }
            else
            {
                problematicUsers.Add($"@{user.TagUserName}");
            }
        }

        if (problematicUsers.Any())
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                $"⚠️ Не удалось связаться со следующими участниками: {string.Join(", ", problematicUsers)}. Попросите их написать команду /start боту в личных сообщениях."
            );
        }

        if (validUsers.Count < 2)
        {
            await _botClient.SendTextMessageAsync(chatId, "❗ Недостаточно доступных участников для распределения пар.");
            return false;
        }

        var shuffled = validUsers.OrderBy(_ => Guid.NewGuid()).ToList();

        for (int i = 0; i < shuffled.Count; i++)
        {
            var giver = shuffled[i];
            var receiver = shuffled[(i + 1) % shuffled.Count];

            try
            {
                await SendMessageAndLogAsync(giver.Telegram_Id, $"🎁 Вы должны подарить подарок @{receiver.TagUserName}!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке сообщения @{giver.TagUserName}: {ex.Message}");
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"⚠️ Ошибка при уведомлении @{giver.TagUserName}. Возможно, бот заблокирован."
                );
            }
        }

        await _botClient.SendTextMessageAsync(chatId, "✅ Все пары распределены и уведомлены в личных сообщениях!");
        return true;
    }

    private async Task<bool> CanCommunicateWithUser(long userId)
    {
        bool messageExists = _dbContext.SendMessages.Any(x => x.Telegram_Id == userId);

        if (!messageExists)
        {
            try
            {
                await _botClient.SendTextMessageAsync(userId, "Проверка доступности бота.");
                return true;
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                if (ex.Message.Contains("Forbidden") || ex.Message.Contains("bot was blocked by the user"))
                {
                    Console.WriteLine($"Бот заблокирован пользователем: {userId}");
                    return false;
                }

                throw;
            }
            catch
            {
                return false;
            }
        }
        else
        {
            return true;
        }
    }

    private async Task SendMessageAndLogAsync(long userId, string message)
    {
        try
        {
            await _botClient.SendTextMessageAsync(userId, message);

            bool messageExists = _dbContext.SendMessages.Any(x => x.Telegram_Id == userId);

            if (!messageExists)
            {
                _dbContext.SendMessages.Add(new SendMessage
                {
                    Telegram_Id = userId,
                    SentAt = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync();
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex)
        {
            if (ex.Message.Contains("Forbidden") || ex.Message.Contains("bot was blocked by the user"))
            {
                Console.WriteLine($"Пользователь заблокировал бота: {userId}");
            }
            else
            {
                Console.WriteLine($"Ошибка отправки сообщения пользователю {userId}: {ex.Message}");
            }
        }
    }

    private async Task SetBotCommands()
    {
        var groupCommands = new List<BotCommand>
        {
            new BotCommand { Command = "start", Description = "Начать новую игру" },
            new BotCommand { Command = "stop", Description = "Завершить регистрацию и распределить пары" },
            new BotCommand { Command = "reset", Description = "Сбросить текущую игру" },
            new BotCommand { Command = "join", Description = "Присоединиться к игре" },
            new BotCommand { Command = "info", Description = "Информация об участниках" },
        }; 
        
        var privateCommands = new List<BotCommand>
        {
            new BotCommand { Command = "start", Description = "Запустить бота" },
            new BotCommand { Command = "info", Description = "Информация о боте" },
        };

        await _botClient.SetMyCommandsAsync(groupCommands, new BotCommandScopeAllGroupChats());
        await _botClient.SetMyCommandsAsync(privateCommands, new BotCommandScopeAllPrivateChats());
    }

    private async Task<bool> ValidateGroupAndAdmin(long gameId, string chatType, long userId, string errorMessage = null)
    {
        if (chatType != "group" && chatType != "supergroup")
        {
            await _botClient.SendTextMessageAsync(gameId, "⚠️ Эта команда доступна только в групповых чатах.");
            return false;
        }

        if (!await IsUserAdmin(gameId, userId))
        {
            await _botClient.SendTextMessageAsync(gameId,
                errorMessage ?? "❌ У вас нет прав администратора для выполнения этой команды.");
            return false;
        }

        return true;
    }

    private async Task<bool> IsUserAdmin(long gameId, long userId)
    {
        try
        {
            var admins = await _botClient.GetChatAdministratorsAsync(gameId);
            return admins.Any(a => a.User.Id == userId);
        }
        catch
        {
            return false;
        }
    }

    private async Task ShowAllParticipants(long gameId, string chatType)
    {
        if (chatType != "group" && chatType != "supergroup")
        {
            await _botClient.SendTextMessageAsync(gameId, "⚠️ Эта команда доступна только в групповых чатах.");
            return;
        }

        var users = await _dbContext.Users
            .Where(p => p.Game_Id == gameId)
            .ToListAsync();

        if (!users.Any())
        {
            await _botClient.SendTextMessageAsync(gameId, "❌ Участники не найдены.");
            return;
        }

        var participantList = string.Join("\n", users.Select(p => $"@{p.TagUserName}"));
        await _botClient.SendTextMessageAsync(gameId, $"👥 Список участников:\n{participantList}");
    }
}
