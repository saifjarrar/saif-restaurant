namespace SaifRestaurantApi.Models
{
    public class Dashboard
    {
        public List<RestaurantInfo> Restaurantinfo { get; set; } = new();
        public List<OrderTicket> Ordertickets { get; set; } = new();
        public List<Reservation> Tonightreservations { get; set; } = new();
        public List<MostOrdered> Mostordered { get; set; } = new();
    }
    public class RestaurantInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Describe { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }
    public class OrderTicket
    {
        public int Id { get; set; }
        public string Ordernum { get; set; } = string.Empty;
        public string Table { get; set; } = string.Empty;
        public List<OrderDish> Dishes { get; set; } = new();
        public string Orderstatus { get; set; } = string.Empty;
        public string Readytime { get; set; } = string.Empty;
        public decimal? Total { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? ReadyAt { get; set; }
    }
    public class OrderDish
    {
        public int Id { get; set; }
        public int OrderTicketId { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    public class Reservation
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Time { get; set; } = "";
        public int Guests { get; set; }
        public string Table { get; set; } = "";
        public string Phone { get; set; } = string.Empty;  
        public DateTime Date { get; set; }
    }
    public class MostOrdered
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}