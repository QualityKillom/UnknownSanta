namespace UnknownSanta.Domain.Entities;

public class Users
{
    // Уникальный идентификатор пользователя
    public int Id { get; set; }
    
    // Тег (псевдоним) пользователя в Telegram
    public string TagUserName { get; set; }
    
    // Участие пользователя в игре (например, участвующий или нет)
    public bool Participle { get; set; }
    
    // Идентификатор игры, к которой привязан пользователь
    public long Game_Id { get; set; }
    
    // Идентификатор пользователя в Telegram
    public long Telegram_Id { get; set; }
}