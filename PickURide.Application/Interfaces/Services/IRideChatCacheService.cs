using PickURide.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Services
{
    public interface IRideChatCacheService
    {
        void SaveMessage(ChatMessageDto message);
        List<ChatMessageDto> GetMessages(Guid rideId);
        /// <summary>Messages with Sequence strictly greater than <paramref name="afterSequence"/>.</summary>
        List<ChatMessageDto> GetMessagesAfter(Guid rideId, long afterSequence);
        void ClearMessages(Guid rideId);
        void SaveDriverAdminMessage(Guid driverId, ChatMessageDto message);
        List<ChatMessageDto> GetDriverAdminMessages(Guid driverId);
        void ClearDriverAdminMessages(Guid driverId);
        IEnumerable<Guid> GetDriverAdminConversationIds();

    }
}
