namespace UnknownSanta.Domain.Entities;

public class Games
{
    public int Id { get; set; }
    
    public int Users_Id { get; set; }
    
    public long Chat_Id { get; set; }
    
    public string Currency { get; set; }
    
    public DateTime DateCreate { get; set; }
    
    public DateTime DateEnd { get; set; }
}