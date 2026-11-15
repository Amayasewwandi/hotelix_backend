using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("services")]
public class ServiceRequest
{
    [Key]
    [Column("order_id")]
    public int OrderId { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Required]
    [Column("service_type")]
    public string ServiceType { get; set; }

    [Column("details")]
    public string Details { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";
}
