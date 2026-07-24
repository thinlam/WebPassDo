namespace PassDo.Application.Favorites.DTOs;

public class FavoriteDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? PrimaryImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
