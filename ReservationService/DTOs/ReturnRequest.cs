using ReservationService.Enums;

namespace ReservationService.DTOs;

public class ReturnRequest
{
    public BookCondition Condition { get; set; }
    public string? Notes { get; set; }
}