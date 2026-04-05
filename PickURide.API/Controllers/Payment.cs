using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using System;

namespace PickURide.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Payment : ControllerBase
    {
        private readonly IPaymentRepository _paymentService;

        public Payment(IPaymentRepository paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        public async Task<IActionResult> MakePayment([FromBody] PaymentDto dto)
        {
            await _paymentService.AddAsync(dto);
            return Ok(new { Message = "Payment processed." });
        }
        [HttpPost("update-payment-paid-admin")]
        public async Task<IActionResult> UpdatePayment([FromBody] PaymentDto dto)
        {
            await _paymentService.UpdateAsync(dto);
            return Ok(new { Message = "Payment updated." });
        }
        [HttpGet("get-payments-by-ride-admin/{rideId}")]
        public async Task<IActionResult> GetPaymentByRideId(Guid rideId)
        {
            var payment = await _paymentService.GetByRideIdAsync(rideId);
            if (payment == null)
            {
                return NotFound(new { Message = "Payment not found." });
            }
            return Ok(payment);
        }
        [HttpPost("get-all-payments-admin")]
        public async Task<IActionResult> GetAllPayments()
        {
            var payments = await _paymentService.GetAllAsync();
            return Ok(payments);
        }
        [HttpGet("get-all-payments-details-admin")]
        public async Task<IActionResult> GetAllPaymentsWithDetails()
        {
            var payments = await _paymentService.GetAllWithDetailsAsync();
            return Ok(payments);
        }
        [HttpPost("get-paged-payments-admin")]
        public async Task<IActionResult> GetPagedPayments([FromBody] PaymentFilterDto filter)
        {
            var result = await _paymentService.GetPagedPaymentsWithDetailsAsync(filter);
            return Ok(result);
        }
        [HttpGet("get-earnings-summary-admin")]
        public async Task<IActionResult> GetEarningsSummary()
        {
            var summary = await _paymentService.GetEarningsSummaryAsync();
            return Ok(summary);
        }
        [HttpPost("customer-payments")]
        public async Task<IActionResult> UpdateCustomerPayments(CustomerPaymentDto dto)
        {
            var payments=await _paymentService.AddCustomerPayment(dto);
            return Ok(payments);
        }
        [HttpGet("driver-transaction/{driverId}")]
        public async Task<IActionResult> GetDriverTransaction(Guid driverId)
        {
            var transaction = await _paymentService.DriverTransaction(driverId);
            return Ok(transaction);
        }
        [HttpGet("driver-paid-transaction-details/{driverId}")]
        public async Task<IActionResult> GetDriverPaidTransactionDetails(Guid driverId)
        {
            var transaction = await _paymentService.DriverPaidTransactionDetails(driverId);
            return Ok(transaction);
        }
        [HttpPost("complete-transaction")]
        public async Task<IActionResult> CompleteTransaction([FromBody] CompleteTransactionRequest request)
        {
            try
            {
                var payment = await _paymentService.CompleteTransactionAsync(request);
                return Ok(new { Message = "Transaction completed successfully.", Payment = payment });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while completing the transaction.", Error = ex.Message });
            }
        }
        [HttpGet("held-payments")]
        public async Task<IActionResult> GetHeldPayments()
        {
            var heldPayments = await _paymentService.GetHeldPaymentsAsync();
            return Ok(heldPayments);
        }
        [HttpPost("held-payments")]
        public async Task<IActionResult> CreateHeldPayment([FromBody] CreateHeldPaymentRequest request)
        {
            try
            {
                var heldPayment = await _paymentService.CreateHeldPaymentAsync(request);
                return Ok(heldPayment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while creating the held payment.", Error = ex.Message });
            }
        }
    }
}
