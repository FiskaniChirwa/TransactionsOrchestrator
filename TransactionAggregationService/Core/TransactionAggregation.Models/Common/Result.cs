namespace TransactionAggregation.Models.Common;

public class Result<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? WarningMessage { get; private set; }
    public string? WarningCode { get; private set; }

    private Result() { }

    public static Result<T> SuccessResult(T data)
    {
        return new Result<T>
        {
            Success = true,
            Data = data
        };
    }

    public static Result<T> SuccessResultWithWarning(T data, string warningMessage, string warningCode)
    {
        return new Result<T>
        {
            Success = true,
            Data = data,
            WarningMessage = warningMessage,
            WarningCode = warningCode
        };
    }

    public static Result<T> FailureResult(string errorMessage, string errorCode)
    {
        return new Result<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

public class Result
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorCode { get; private set; }

    private Result() { }

    public static Result SuccessResult()
    {
        return new Result
        {
            Success = true
        };
    }

    public static Result Failure(string errorMessage, string errorCode)
    {
        return new Result
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}