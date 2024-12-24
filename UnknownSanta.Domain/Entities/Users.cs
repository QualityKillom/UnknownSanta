
namespace UnknownSanta.Domain.Entities;

public class Users
{
    public int Id { get; set; }
    
    public string TagUserName { get; set; }
    
    public int Role_Id { get; set; }
    
    public bool Participle { get; set; }
    
    public int Game_Id { get; set; }
    
    public long Chat_Id { get; set; }
}