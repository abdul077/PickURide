using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        IRideRepository RideRepository { get; }
        IDriverRepository DriverRepository { get; }
        IUserRepository UserRepository { get; }
        IFeedbackRepository FeedbackRepository { get; }
        //IPaymentRepository PaymentRepository { get; }
        ITipRepository TipRepository { get; }
        IRideMessageRepository RideMessageRepository { get; }
        IFareSettingRepository  FareSettingRepository { get; }
        IPromoRepository PromoRepository { get; }
        Task<int> SaveAsync();
    }
}
