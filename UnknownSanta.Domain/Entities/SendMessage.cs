namespace UnknownSanta.Domain.Entities;

public class SendMessage
{
    public int Id { get; set; }
    public long Telegram_Id { get; set; }
    public DateTime SentAt { get; set; }
}
