using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using saifRestaurantApi.Dto;
using SaifRestaurantApi.Data;
using SaifRestaurantApi.DTOs;
using SaifRestaurantApi.Models;
using SaifRestaurantApi.Models;
using SaifRestaurantApi.Utility;
using System.Globalization;

namespace SaifRestaurantApi.Controllers;

[ApiController]

[Route("api/[controller]")]

public class DashboardController : ControllerBase

{

    private readonly AppDBContext _db;


    public DashboardController(AppDBContext db) { _db = db; }
    [HttpGet]
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<IActionResult> Get()
    {
        var tickets = await _db.OrderTickets.Include(t => t.Dishes).ToListAsync();
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var todayReservations = await _db.Reservations
            .Where(r => r.Date >= today && r.Date < tomorrow)
            .ToListAsync();

        var reservations = todayReservations
            .OrderBy(r => ParseTime(r.Time))
            .ToList();


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
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<IActionResult> Advance(int id)
    {
        var order = await _db.OrderTickets.AsNoTracking()
            .Include(t => t.Dishes)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (order is null)
            return NotFound(new { message = "Order not found." });

        var currentStatus = order.Orderstatus;
        var nextStatus = currentStatus switch
        {
            "Received" => "In The Kitchen",
            "In The Kitchen" => "Served",
            "Served" => "Received",
            _ => null
        };

        if (nextStatus is null)
            return Conflict(new { message = "This order cannot change status." });

        var updated = await _db.OrderTickets
            .Where(t => t.Id == id && t.Orderstatus == currentStatus)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Orderstatus, nextStatus));

        if (updated == 0)
            return Conflict(new { message = "Order status changed. Refresh the dashboard." });

        order.Orderstatus = nextStatus;
        return Ok(order);
    }

    [HttpPut("orders/{id}/cancel")]
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<IActionResult> Cancel(int id)
    {
        await _db.OrderTickets
            .Where(t => t.Id == id &&
                (t.Orderstatus == "Received" || t.Orderstatus == "In The Kitchen"))
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Orderstatus, "Cancelled"));

        var order = await _db.OrderTickets.AsNoTracking()
            .Include(t => t.Dishes)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (order is null)
            return NotFound(new { message = "Order not found." });

        if (order.Orderstatus != "Cancelled")
            return Conflict(new { message = "Only active orders can be cancelled." });

        return Ok(order);
    }
    [HttpPost("orders")]
    [Authorize(Roles = SD.Role_Admin)]
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
   


    private static DateTime ParseTime(string time)
    {
        return DateTime.TryParseExact(
            time, "h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : DateTime.MaxValue; 
    }


    [HttpPost("reservations")]
    [Authorize]
    public async Task<IActionResult> CreateReservation(CreateReservationDto request)
    {
        
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Please enter your name" });
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return BadRequest(new { message = "Please enter a phone number" });
        }

        if (request.Guests < 1 || request.Guests > 8)
        {
            return BadRequest(new { message = "Guests must be between 1 and 8" });
        }

        if (request.Date.Date < DateTime.Today)
        {
            return BadRequest(new { message = "The date cannot be in the past" });
        }

        if (ParseTime(request.Time) == DateTime.MaxValue)
        {
            return BadRequest(new { message = "Please choose a valid time" });
        }

        if (string.IsNullOrWhiteSpace(request.Seating))
        {
            return BadRequest(new { message = "Please choose a seating area" });
        }

        
        var reservation = new Reservation
        {
            Name = request.Name.Trim(),
            Phone = request.Phone.Trim(),
            Date = request.Date.Date,
            Time = request.Time,
            Guests = request.Guests,
            Table = request.Seating,
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        return Ok(reservation);
    }




}
