using System;

namespace Problem1
{
    public class Order
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ProcessedAt { get; set; }
        public string PatientEmail { get; set; } = string.Empty;
    }

    public interface IOrderRepository
    {
        Order? GetById(int orderId);
        void Update(Order order);
    }

    public interface IEmailService
    {
        void Send(string to, string message);
    }

    public interface IDateTimeProvider
    {
        DateTime Now { get; }
    }
}
