using Microsoft.AspNetCore.SignalR;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Hub;
using PickURide.Infrastructure.Repositories;

namespace PickURide.Infrastructure.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PickURideDbContext _context;
        private readonly IHubContext<RideChatHub> hubContext;
        public UnitOfWork(PickURideDbContext context, IHubContext<RideChatHub> hubContext)
        {
            _context = context;
            this.hubContext = hubContext;

            RideRepository = new RideRepository(_context);
            DriverRepository = new DriverRepository(_context);
            FeedbackRepository = new FeedbackRepository(_context);
            //PaymentRepository = new PaymentRepository(_context,hubContext);
            TipRepository = new TipRepository(_context);
            RideMessageRepository = new RideMessageRepository(_context);
            FareSettingRepository = new FareSettingRepository(_context);
            UserRepository = new UserRepository(_context);
            SupportChatRepository = new SupportChatRepository(_context);
            PromoRepository = new PromoRepository(_context);
        }

        public IRideRepository RideRepository { get; private set; }
        public IDriverRepository DriverRepository { get; private set; }
        public IUserRepository UserRepository { get; private set; }
        public IFeedbackRepository FeedbackRepository { get; private set; }
        //public IPaymentRepository PaymentRepository { get; private set; }
        public ITipRepository TipRepository { get; private set; }
        public IRideMessageRepository RideMessageRepository { get; private set; }
        public IFareSettingRepository FareSettingRepository { get; private set; }
        public ISupportChatRepository SupportChatRepository { get; private set; }
        public IPromoRepository PromoRepository { get; private set; }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
