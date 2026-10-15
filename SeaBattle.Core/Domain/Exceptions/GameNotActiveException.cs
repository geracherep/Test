namespace SeaBattle.Core.Domain.Exceptions;

public class GameNotActiveException : DomainException
{
    public GameNotActiveException() : base("Игра не активна") { }
}
