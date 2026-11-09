using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaifRestaurantApi.Data;
using SaifRestaurantApi.DTOs;
using SaifRestaurantApi.Models;
using SaifRestaurantApi.Models;

namespace SaifRestaurantApi.Controllers;

[ApiController]

[Route("api/[controller]")]

public class DashboardController : ControllerBase

{

    private readonly AppDBContext _db;


    public DashboardController(AppDBContext db) { _db = db; }
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var tickets = await _db.OrderTickets.Include(t => t.Dishes).ToListAsync();
        var reservations = await _db.Reservations.ToListAsync();

        var mostOrdered = await _db.OrderDishes
            .GroupBy(d => d.Name)
            .Select(g => new MostOrdered { Name = g.Key, Quantity = g.Sum(d => d.Quantity) })
            .OrderByDescending(m => m.Quantity)
            .Take(5)
            .ToListAsync();

        return Ok(new Dashboard
        {
            Ordertickets = tickets,
            Tonightreservations = reservations,
            Mostordered = mostOrdered,
            Restaurantinfo = new()
        {
            new RestaurantInfo { Title = "Orders today", Value = tickets.Count.ToString(), Describe = "Orders on the board", Color = "up" },
            new RestaurantInfo { Title = "Reservations", Value = reservations.Count.ToString(), Describe = "Booked for tonight", Color = "" },
            new RestaurantInfo { Title = "Sales", Value = "$1,250", Describe = "8% more than last Monday", Color = "up" },
            new RestaurantInfo { Title = "Guest rating", Value = "4.5", Describe = "Average rating from customers", Color = "" },
        },
        });
    }
    [HttpPut("orders/{id}/advance")]
    public async Task<IActionResult> Advance(int id)
    {
        var order = await _db.OrderTickets
            .Include(t => t.Dishes)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (order is null)
            return NotFound(new { message = "Order not found" });

        if (order.Orderstatus == "Received")
        {
            order.Orderstatus = "In The Kitchen";
        }
        else if (order.Orderstatus == "In The Kitchen")
        {
            order.Orderstatus = "Served";
        }
        else if(order.Orderstatus=="Served")
        {
            order.Orderstatus = "Received";
        }

        await _db.SaveChangesAsync();

        return Ok(order);
    }
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder(CreateOrderDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Table))
        {
            return BadRequest(new { message = "Please choose a table" });
        }

        var validDishes = request.Dishes
            .Where(d => !string.IsNullOrWhiteSpace(d.Name) && d.Quantity > 0)
            .ToList();

   
        if (validDishes.Count == 0)
        {
            return BadRequest(new { message = "Please add at least one dish" });
        }

        var order = new OrderTicket
        {
            Ordernum = string.Empty,        
            Table = request.Table,
            Orderstatus = "Received",
            Readytime = "20 mins",
            Dishes = validDishes
                .Select(d => new OrderDish { Name = d.Name, Quantity = d.Quantity })
                .ToList(),
        };

        
        _db.OrderTickets.Add(order);
        await _db.SaveChangesAsync();

        order.Ordernum = "#" + (2480 + order.Id);
        await _db.SaveChangesAsync();

        return Ok(order);
    }

}