using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SpeakingClub.Hubs
{
    // Live quiz submissions for /QuizMonitor. Results are personal data: only staff may connect.
    [Authorize(Roles = "Root,Admin,Teacher")]
    public class QuizMonitorHub : Hub
    {
    }
}