using PassDo.Application.Common.Interfaces;

namespace PassDo.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
