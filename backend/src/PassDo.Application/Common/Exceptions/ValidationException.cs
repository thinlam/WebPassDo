namespace PassDo.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base(FirstMessage(errors))
    {
        Errors = errors;
    }

    private static string FirstMessage(IDictionary<string, string[]> errors)
    {
        var first = errors.Values.SelectMany(x => x).FirstOrDefault();
        return string.IsNullOrWhiteSpace(first)
            ? "Dữ liệu không hợp lệ."
            : first;
    }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>
        {
            { "Error", new[] { message } }
        };
    }
}
