namespace SeaBattle.Core.Domain.Exceptions;

public class NotYourMoveException : DomainException
{
    public NotYourMoveException() : base("Сейчас не ваш ход") { }
    public NotYourMoveException(string message) : base(message) { }
}
