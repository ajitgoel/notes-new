using Moq;
using Xunit;
using System;

namespace Problem1.Tests
{
    public class OrderProcessorTests
    {
        private readonly Mock<IOrderRepository> _mockRepo;
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<IDateTimeProvider> _mockClock;
        private readonly OrderProcessor _processor;

        public OrderProcessorTests()
        {
            _mockRepo = new Mock<IOrderRepository>();
            _mockEmail = new Mock<IEmailService>();
            _mockClock = new Mock<IDateTimeProvider>();
            
            _processor = new OrderProcessor(
                _mockRepo.Object, 
                _mockEmail.Object, 
                _mockClock.Object);
        }

        [Fact]
        public void ProcessOrder_ValidOrder_UpdatesStatusAndSendsEmail()
        {
            // Arrange
            int orderId = 123;
            var order = new Order { Id = orderId, PatientEmail = "test@example.com", Status = "New" };
            var fixedTime = new DateTime(2026, 1, 1);

            _mockRepo.Setup(r => r.GetById(orderId)).Returns(order);
            _mockClock.Setup(c => c.Now).Returns(fixedTime);

            // Act
            _processor.ProcessOrder(orderId);

            // Assert
            Assert.Equal("Processed", order.Status);
            Assert.Equal(fixedTime, order.ProcessedAt);
            
            _mockRepo.Verify(r => r.Update(It.Is<Order>(o => o.Id == orderId)), Times.Once);
            _mockEmail.Verify(e => e.Send(order.PatientEmail, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void ProcessOrder_OrderNotFound_ThrowsException()
        {
            // Arrange
            int orderId = 999;
            _mockRepo.Setup(r => r.GetById(orderId)).Returns((Order?)null);

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => _processor.ProcessOrder(orderId));
            Assert.Equal("Order not found", ex.Message);
        }
    }
}
