namespace PickURide.Application.Models.AllRides
{
    public class AllRidesDto
    {
        public Guid RideId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? DriverId { get; set; }
        public string? RideType { get; set; }
        public bool? IsScheduled { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public int? PassengerCount { get; set; }
        public decimal? FareEstimate { get; set; }
        public decimal? FareFinal { get; set; }
        public string? Status { get; set; }
        public string? UserName { get; set; }
        public string? DriverName { get; set; }
        public string? VehicleName { get; set; }
        public string? VehicleColor { get; set; }
        public DateTime? CreatedAt { get; set; }
        public TimeOnly TotalWaitingTime { get; set; }
        public TimeOnly RideStartTime { get; set; }
        public TimeOnly RideEndTime { get; set; }
        public double Distance { get; set; }
        public List<RideFeedbackDto> Feedbacks { get; set; } = new();
        public List<RidePaymentDto> Payments { get; set; } = new();
        public List<RideMessageDto> RideMessages { get; set; } = new();
        public List<RideStopsDto> RideStops { get; set; } = new();
        public List<RideTipDto> Tips { get; set; } = new();
    }

    public class RideFeedbackDto
    {
        public Guid FeedbackId { get; set; }
        public Guid RideId { get; set; }
        public string? Comment { get; set; }
        public int? Rating { get; set; }
    }

    public class RidePaymentDto
    {
        public Guid PaymentId { get; set; }
        public Guid RideId { get; set; }
        public decimal Amount { get; set; }
        public string? Method { get; set; }
    }

    public class RideMessageDto
    {
        public Guid MessageId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; }
        public string SenderRole { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
    }

    public class RideStopsDto
    {
        public Guid RideStopId { get; set; }
        public int? StopOrder { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class RideTipDto
    {
        public Guid TipId { get; set; }
        public Guid RideId { get; set; }
        public decimal Amount { get; set; }
    }
}

