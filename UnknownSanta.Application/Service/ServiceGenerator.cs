using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using UnknownSanta.Infrastructure;

namespace UnknownSantaa.Application.Service;

public class ServiceGenerator
{
    private static List<(long Id, string? Username)> participants = new();
    private static bool gameStarted = false;

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is { } message)
        {
            var chatId = message.Chat.Id;
            var userId = message.From.Id;
            var username = message.From.Username ?? "без имени";
            Console.WriteLine($"Получено сообщение '{message.Text}' в чате {chatId}.");

            var botInfo = await botClient.GetMeAsync();
            var botUsername = $"@{botInfo.Username.ToLower()}";
            var text = message.Text.ToLower().Replace(botUsername, "").Trim();

            switch (text)
            {
                case "/start":
                    gameStarted = true;
                    participants.Clear();
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Игра Тайный Санта началась! Напишите /join, чтобы участвовать, и /stop, чтобы закончить.",
                        cancellationToken: cancellationToken
                    );
                    break;

                case "/join":
                    if (gameStarted && !participants.Any(p => p.Id == userId))
                    {
                        participants.Add((userId, username));
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"Вы добавлены в список участников как @{username}!",
                            cancellationToken: cancellationToken
                        );
                    }
                    else if (!gameStarted)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Игра еще не началась. Введите /start для начала.",
                            cancellationToken: cancellationToken
                        );
                    }
                    break;

                case "/stop":
                    if (gameStarted && participants.Count > 1)
                    {
                        gameStarted = false;
                        await AssignGifters(botClient, cancellationToken);
                        participants.Clear();
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Игра не может быть завершена. Добавьте участников с помощью /join.",
                            cancellationToken: cancellationToken
                        );
                    }
                    break;

                default:
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"Вы написали: {message.Text}",
                        cancellationToken: cancellationToken
                    );
                    break;
            }
        }
    }

    private async Task AssignGifters(ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var shuffled = participants.OrderBy(_ => Guid.NewGuid()).ToList();

        for (int i = 0; i < shuffled.Count; i++)
        {
            var receiver = shuffled[(i + 1) % shuffled.Count];
            var receiverInfo = await botClient.GetChatAsync(receiver.Id, cancellationToken);
            var receiverUsername = receiverInfo.Username ?? "пользователь без имени";

            await botClient.SendTextMessageAsync(
                chatId: shuffled[i].Id,
                text: $"Ваш получатель подарка — @{receiverUsername}",
                cancellationToken: cancellationToken
            );
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Ошибка Telegram API:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
            _ => exception.ToString()
        };
        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}
