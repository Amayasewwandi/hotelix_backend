using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class BookingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BookingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Booking>>> GetBookings()
    {
        return await _context.Bookings
            .AsNoTracking()
            .OrderByDescending(b => b.CheckIn)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Booking>> PostBooking(Booking booking)
    {
        // The booking form sends calendar dates. Compare date-only values so
        // browser time zones cannot create accidental one-day overlaps.
        var checkIn = booking.CheckIn.Date;
        var checkOut = booking.CheckOut.Date;

        if (booking.RoomId is null || booking.RoomId <= 0)
            return BadRequest(new { message = "Please select a valid room." });

        if (checkIn == default || checkOut <= checkIn)
            return BadRequest(new { message = "Check-out date must be after the check-in date." });

        var roomExists = await _context.Rooms.AnyAsync(r => r.RoomId == booking.RoomId);
        if (!roomExists)
            return BadRequest(new { message = "This room does not exist in the database. Please refresh and select another room." });

        // Ignore cancelled/rejected bookings. A room is occupied only when
        // another active booking overlaps the requested date range.
        var activeStatuses = new[] { "Confirmed", "Pending", "Booked" };
        var alreadyBooked = await _context.Bookings.AnyAsync(b =>
            b.RoomId == booking.RoomId &&
            activeStatuses.Contains(b.Status) &&
            b.CheckIn.Date < checkOut &&
            b.CheckOut.Date > checkIn);

        if (alreadyBooked)
            return Conflict(new { message = "This room is already booked for at least one of the selected nights. Please choose different dates." });

        booking.BookingId = 0;
        booking.CheckIn = checkIn;
        booking.CheckOut = checkOut;
        booking.Status = "Confirmed";

        // user_id is nullable in the existing SQL schema. Do not invent a
        // user ID when the current frontend session has no database user ID.
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBookings), new { id = booking.BookingId }, booking);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBooking(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null) return NotFound();

        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
