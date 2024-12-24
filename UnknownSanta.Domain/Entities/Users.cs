
namespace UnknownSanta.Domain.Entities;

public class Users
{
    public int Id { get; set; }
    
    public string TagUserName { get; set; }
    
    public bool Participle { get; set; }
    public long Game_Id { get; set; }
    
    public long Telegram_Id { get; set; }
}