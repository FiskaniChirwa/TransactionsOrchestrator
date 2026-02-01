namespace FraudEngine.Core.Common;

public class Result<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public string ErrorCode { get; private set; } = string.Empty;

    public static Result<T> SuccessResult(T data) => new()
    {
        Success = true,
        Data = data
    };

    public static Result<T> Failure(string errorMessage, string errorCode) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode
    };
}

public class Result
{
    public bool Success { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public string ErrorCode { get; private set; } = string.Empty;

    public static Result SuccessResult() => new()
    {
        Success = true
    };

    public static Result Failure(string errorMessage, string errorCode) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode
    };
}