using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Services
{
    public interface IEmailOTPService
    {
        Task SendOtpAsync(string email, string otp);
        bool VerifyOtp(string email, string otp);
    }
}
