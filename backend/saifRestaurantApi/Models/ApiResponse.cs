using System.Net;

namespace SaifRestaurantApi.Models
{
    public class ApiResponse
    {
        public HttpStatusCode Status { get; set; }
        public bool IsSuccess { get; set; } = true;
        public List<string> Errors { get; set; } = [];
        public object? Result { get; set; }

    }
}