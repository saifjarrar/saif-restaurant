namespace SaifRestaurantApi.DTOs
{
  
    public class CreateOrderDishDto
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

   
    public class CreateOrderDto
    {
        public string Table { get; set; } = string.Empty;
        public List<CreateOrderDishDto> Dishes { get; set; } = new();
    }
}