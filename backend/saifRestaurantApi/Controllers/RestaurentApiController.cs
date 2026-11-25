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
        // Define the business day using Jordan local time (midnight to midnight).
        var jordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman");
        var jordanNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, jordanTimeZone);
        var jordanDayStart = new DateTime(
            jordanNow.Year, jordanNow.Month, jordanNow.Day, 0, 0, 0,
            DateTimeKind.Unspecified);
        var jordanDayEnd = jordanDayStart.AddDays(1);

        // Order timestamps are stored in UTC, so convert Jordan midnight boundaries to UTC.
        var startUtc = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(jordanDayStart, jordanTimeZone), TimeSpan.Zero);
        var endUtc = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(jordanDayEnd, jordanTimeZone), TimeSpan.Zero);

        // Sales are calculated from persisted order totals. Cancelled orders are excluded,
        // so cancelling an order automatically removes its value from the dashboard.
        var lastWeekStartUtc = startUtc.AddDays(-7);
        var lastWeekEndUtc = endUtc.AddDays(-7);
        var todaySales = await _db.OrderTickets
            .Where(t => t.CreatedAt.HasValue &&
                        t.CreatedAt.Value >= startUtc &&
                        t.CreatedAt.Value < endUtc &&
                        t.Orderstatus != "Cancelled")
            .SumAsync(t => t.Total ?? 0m);
        var lastWeekSales = await _db.OrderTickets
            .Where(t => t.CreatedAt.HasValue &&
                        t.CreatedAt.Value >= lastWeekStartUtc &&
                        t.CreatedAt.Value < lastWeekEndUtc &&
                        t.Orderstatus != "Cancelled")
            .SumAsync(t => t.Total ?? 0m);

        var salesChange = lastWeekSales > 0
            ? (todaySales - lastWeekSales) / lastWeekSales * 100m
            : 0m;
        var salesChangeText = lastWeekSales > 0
            ? $"{(salesChange > 0 ? "+" : "")}{salesChange:0.#}%"
            : todaySales > 0 ? "New" : "0%";
        var salesDescription = lastWeekSales == 0 && todaySales == 0
            ? "No sales on same day last week"
            : "compared with same day last week";

        var tickets = await _db.OrderTickets
            .Where(t => t.CreatedAt.HasValue &&
                        t.CreatedAt.Value >= startUtc &&
                        t.CreatedAt.Value < endUtc)
            .Include(t => t.Dishes)
            .ToListAsync();

        var todayReservations = await _db.Reservations
            .Where(r => r.Date >= jordanDayStart && r.Date < jordanDayEnd)
            .ToListAsync();

        var reservations = todayReservations
            .OrderBy(r => ParseTime(r.Time))
            .ToList();

        var todayOrderIds = tickets.Select(t => t.Id).ToList();
        var mostOrdered = await _db.OrderDishes
            .Where(d => todayOrderIds.Contains(d.OrderTicketId))
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
            new RestaurantInfo { Title = "Sales", Value = $"{todaySales:N2}", Change = salesChangeText, Describe = salesDescription, Color = salesChange > 0 || (lastWeekSales == 0 && todaySales > 0) ? "up" : "" },
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
    private static readonly IReadOnlyDictionary<string, decimal> MenuPrices =
    new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
    {
        ["Hummus"] = 1.99m,
        ["Falafel"] = 0.99m,
        ["Fattoush Salad"] = 2.99m,
        ["Mutabbal"] = 2.49m,
        ["Kibbeh"] = 3.99m,
        ["Chicken Shawarma"] = 5.99m,
        ["Beef Kofta"] = 6.99m,
        ["Mansaf"] = 9.99m,
        ["Lamb Tagine"] = 7.99m,
        ["Beef Burger"] = 7.49m,
        ["Grilled Chicken"] = 8.99m,
        ["Grilled Salmon"] = 12.99m,
        ["Grilled sea bass"] = 13.99m,
        ["Shish Tawook"] = 9.99m,
        ["Grilled Lamb Chops"] = 14.99m,
        ["Knafeh"] = 4.99m,
        ["Baklava"] = 3.99m,
        ["Chocolate Mousse"] = 5.99m,
        ["Rice Pudding"] = 2.99m,
        ["Mint Lemonade"] = 1.99m,
        ["Pomegranate Juice"] = 2.49m,
        ["Turkish Coffee"] = 2.99m,
        ["Arabic Tea"] = 1.49m,
        ["Rose Water Lemonade"] = 2.99m,
    };
    [HttpPost("orders")]
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<IActionResult> CreateOrder(CreateOrderDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Table))
            return BadRequest(new { message = "Please choose a table." });

        if (request.Dishes is null || request.Dishes.Count == 0)
            return BadRequest(new { message = "Please add at least one dish." });

        var dishes = new List<OrderDish>();
        decimal total = 0;

        foreach (var dish in request.Dishes)
        {
            if (dish is null || string.IsNullOrWhiteSpace(dish.Name) || dish.Quantity <= 0)
            {
                return BadRequest(new
                {
                    message = "Choose a dish and enter a quantity of at least 1."
                });
            }

            var name = dish.Name.Trim();

            if (!MenuPrices.TryGetValue(name, out var unitPrice))
                return BadRequest(new { message = $"Unknown dish: {name}" });

            dishes.Add(new OrderDish
            {
                Name = name,
                Quantity = dish.Quantity,
                UnitPrice = unitPrice,
            });
            
    total += unitPrice * dish.Quantity;
        }

        var createdAt = DateTimeOffset.UtcNow;
        const int preparationMinutes = 20;

        var order = new OrderTicket
        {
            Table = request.Table.Trim(),
            Orderstatus = "Received",
            Readytime = $"{preparationMinutes} mins",
            CreatedAt = createdAt,
            ReadyAt = createdAt.AddMinutes(preparationMinutes),
            Total = total,
            Dishes = dishes,
        };

        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.OrderTickets.Add(order);
        await _db.SaveChangesAsync();

        order.Ordernum = "#" + (2480 + order.Id);
        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(order);
    }
    private static DateTime ParseTime(string time)
    {
        return DateTime.TryParseExact(
            time,
            "h:mm tt",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
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
