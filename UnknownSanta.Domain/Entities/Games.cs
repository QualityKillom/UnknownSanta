using UnknownSanta.Domain.ValueObjects.Enums;

namespace UnknownSanta.Domain.Entities;

public class Games
{
    // Идентификатор игры
    public long Game_Id { get; set; }
    
    // Валюта, используемая в игре
    public string Currency { get; set; }
    
    // Тип чата, используемый в игре (например, текстовый чат)
    public string ChatType { get; set; }
    
    // Сумма, связанная с игрой (например, сумма выигрыша или ставка)
    public decimal Amount { get; set; }
    
    // Список участников игры (пользователи)
    public List<Users> Users { get; set; } = new List<Users>();
    
    // Текущее состояние игры, по умолчанию - "Не начата"
    public GameState GameState { get; set; } = GameState.NotStarted;
}