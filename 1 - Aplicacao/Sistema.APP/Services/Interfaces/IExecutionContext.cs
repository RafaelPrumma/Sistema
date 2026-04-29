namespace Sistema.APP.Services.Interfaces;

public interface IExecutionContext
{
    string? Usuario { get; }
    string? CorrelationId { get; }
}
