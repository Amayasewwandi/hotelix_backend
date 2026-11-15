using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("rooms")]
public class Room
{
    [Key]
    [Column("room_id")]
    public int RoomId { get; set; }

    [Required]
    [Column("room_type")]
    public string RoomType { get; set; }

    [Column("price", TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Available";
}
