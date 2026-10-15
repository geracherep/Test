namespace SeaBattle.Core.Domain.Exceptions;

public class InvalidPlacementException : DomainException
{
    public InvalidPlacementException(string message) : base(message) { }
}
