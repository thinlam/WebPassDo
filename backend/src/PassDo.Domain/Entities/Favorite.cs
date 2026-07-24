using PassDo.Domain.Common;

namespace PassDo.Domain.Entities;

public class Favorite : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }

    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
