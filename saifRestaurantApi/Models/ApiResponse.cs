using System.Net;

namespace SaifRestaurantApi.Models
{
    public class ApiResponse
    {
        public HttpStatusCode Status { get; set; }
        public bool IsSuccess = true;
        public List<string> Errors = [];
        public object? Result { get; set; }

    }
}