namespace Api.Services;

public enum ResultStatus { Success, ValidationFailed, NotFound }

// Lets the service report outcome (and validation errors) without knowing about HTTP.
public record ServiceResult<T>(
    ResultStatus Status,
    T? Value = default,
    IDictionary<string, string[]>? Errors = null)
{
    public static ServiceResult<T> Ok(T value) => new(ResultStatus.Success, value);
    public static ServiceResult<T> Invalid(IDictionary<string, string[]> errors) =>
        new(ResultStatus.ValidationFailed, Errors: errors);
    public static ServiceResult<T> NotFound() => new(ResultStatus.NotFound);
}
