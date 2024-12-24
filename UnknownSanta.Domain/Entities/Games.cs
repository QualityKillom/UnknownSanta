using HoHoBot.Domain.ValueObjects.Enums;

namespace UnknownSanta.Domain.Entities;

public class Games
{
    public long Game_Id { get; set; }
    public string Currency { get; set; }
    public string ChatType { get; set; }
    public decimal Amount { get; set; }
    
    public List<Users> Users { get; set; } = new List<Users>();
    public GameState GameState { get; set; } = GameState.NotStarted;
}