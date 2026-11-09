namespace saifRestaurantApi.Dto
{
    public class CreateReservationDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime Date { get; set; }                 
        public string Time { get; set; } = string.Empty;   
        public int Guests { get; set; }
        public string Seating { get; set; } = string.Empty; 
    }
}
