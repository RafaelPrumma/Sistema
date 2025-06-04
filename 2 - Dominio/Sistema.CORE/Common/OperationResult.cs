namespace Sistema.CORE.Common;

public record OperationResult(bool Success, string Message);

public record OperationResult<T>(bool Success, string Message, T? Data = default);
