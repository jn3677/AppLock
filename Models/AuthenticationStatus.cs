using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Models
{
    public class AuthenticationStatus
    {
        public bool IsInLockout { get; set; }
        public int FailedAttempts { get; set; }
        public int MaxAttempts { get; set; }
        public TimeSpan RemainingLockoutTime { get; set; }
        public bool WindowsHelloEnabled { get; set; }
        public bool WindowsHelloAvailable { get; set; }

        public int RemainingAttempts => Math.Max(0, MaxAttempts - FailedAttempts);
        public bool HasRemainingAttempts => RemainingAttempts > 0;
    }
}
