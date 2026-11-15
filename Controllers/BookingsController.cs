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
        return await _context.Bookings.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Booking>> PostBooking(Booking booking)
    {
        try
        {
            // Date overlap validation (සංසන්දනය කිරීම පහසු වීමට .Date භාවිතා කිරීම)
            var existingBooking = await _context.Bookings
                .Where(b => b.RoomId == booking.RoomId &&
                            b.CheckIn.Date < booking.CheckOut.Date &&
                            b.CheckOut.Date > booking.CheckIn.Date)
                .FirstOrDefaultAsync();

            if (existingBooking != null)
            {
                return BadRequest(new { message = "This room is already booked for the selected dates. Please choose different dates." });
            }

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBookings), new { id = booking.BookingId }, booking);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Database error: " + ex.Message });
        }
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
