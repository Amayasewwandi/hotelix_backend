using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("bookings")]
public class Booking
{
    [Key]
    [Column("booking_id")]
    public int BookingId { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("room_id")]
    public int? RoomId { get; set; }

    [Required]
    [Column("check_in")]
    public DateTime CheckIn { get; set; }

    [Required]
    [Column("check_out")]
    public DateTime CheckOut { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Confirmed";
}
