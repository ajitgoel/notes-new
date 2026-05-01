using System;

namespace Problem1
{
    public class OrderProcessor
    {
        private readonly IOrderRepository _repository;
        private readonly IEmailService _emailService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public OrderProcessor(
            IOrderRepository repository, 
            IEmailService emailService, 
            IDateTimeProvider dateTimeProvider)
        {
            _repository = repository;
            _emailService = emailService;
            _dateTimeProvider = dateTimeProvider;
        }

        public void ProcessOrder(int orderId)
        {
            var order = _repository.GetById(orderId);

            if (order == null) throw new Exception("Order not found");

            order.Status = "Processed";
            order.ProcessedAt = _dateTimeProvider.Now;

            _repository.Update(order);

            _emailService.Send(order.PatientEmail, "Your order has been processed!");
        }
    }
}
