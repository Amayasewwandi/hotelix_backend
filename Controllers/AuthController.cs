using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuthController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var email = request.Email.Trim();
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "The email or password is incorrect." });

        if (!string.IsNullOrWhiteSpace(request.Role) &&
            !string.Equals(user.Role, request.Role.Trim(), StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { message = "This account does not have the selected role." });

        // Existing SQL data contains legacy plain-text passwords. Upgrade the
        // password to PBKDF2 after the first successful login.
        if (!user.PasswordHash.StartsWith("pbkdf2-sha256$", StringComparison.Ordinal))
        {
            user.PasswordHash = HashPassword(request.Password);
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            userId = user.UserId,
            name = user.Name,
            email = user.Email,
            role = user.Role
        });
    }

    private static bool VerifyPassword(string password, string storedPassword)
    {
        if (!storedPassword.StartsWith("pbkdf2-sha256$", StringComparison.Ordinal))
        {
            var suppliedBytes = Encoding.UTF8.GetBytes(password);
            var storedBytes = Encoding.UTF8.GetBytes(storedPassword);
            return suppliedBytes.Length == storedBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(suppliedBytes, storedBytes);
        }

        var parts = storedPassword.Split('$');
        if (parts.Length != 4 ||
            !int.TryParse(parts[1], out var iterations) ||
            iterations < 100000)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string HashPassword(string password)
    {
        const int iterations = 210000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, HashAlgorithmName.SHA256, 32);

        return $"pbkdf2-sha256${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? Role { get; set; }
}
