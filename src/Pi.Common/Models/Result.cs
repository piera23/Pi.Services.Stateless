namespace Pi.Common.Models;

public record ValidationError(string PropertyName, string ErrorMessage);

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public IReadOnlyList<ValidationError> Errors { get; }

    protected Result(bool isSuccess, string? error, IEnumerable<ValidationError>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors?.ToList() ?? new List<ValidationError>();
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result ValidationFailure(IEnumerable<ValidationError> errors) => new(false, "Errore di validazione", errors);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, T? value, string? error, IEnumerable<ValidationError>? errors = null)
        : base(isSuccess, error, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public new static Result<T> Failure(string error) => new(false, default, error);
    public new static Result<T> ValidationFailure(IEnumerable<ValidationError> errors) => new(false, default, "Errore di validazione", errors);
}
