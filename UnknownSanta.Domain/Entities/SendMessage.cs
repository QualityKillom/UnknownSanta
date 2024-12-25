namespace UnknownSanta.Domain.Entities;

public class SendMessage
{
    // Уникальный идентификатор сообщения
    public int Id { get; set; }
    
    // Идентификатор пользователя в Telegram, которому отправлено сообщение
    public long Telegram_Id { get; set; }
    
    // Дата и время, когда сообщение было отправлено
    public DateTime SentAt { get; set; }
}