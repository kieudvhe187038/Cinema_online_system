namespace Cinema_System.Application.DTOs;

public class FoodBeverageDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public string? StockStatus { get; set; }
    public bool HasOrders { get; set; }
}
