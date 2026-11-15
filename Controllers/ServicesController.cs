using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class ServicesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ServicesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceRequest>>> GetServices()
    {
        return await _context.Services.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<ServiceRequest>> PostService(ServiceRequest service)
    {
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetServices), new { id = service.OrderId }, service);
    }

    // Guests may cancel their own requests only while they are still Pending.
    // Full identity verification requires the JWT authentication planned for a later security update.
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> CancelService(int id, [FromQuery] int userId)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null) return NotFound(new { message = "Order or service request not found." });
        if (service.UserId != userId) return Forbid();

        if (!string.Equals(service.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "This order can no longer be cancelled because it is already being processed." });

        service.Status = "Cancelled";
        await _context.SaveChangesAsync();
        return Ok(new { message = "Order cancelled successfully.", orderId = service.OrderId, status = service.Status });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteService(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null) return NotFound();

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
