using AppLock.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials.UI;

namespace AppLock.Services
{
    internal class WindowsHelloService : IAuthService
    {
        public string Name => "Windows Hello";

        /// <summary>
        /// Checks if Windows Hello is available for authentication on device.
        /// </summary>
        /// <returns> bool </returns>
        public bool IsAvailable()
        {
            var availability = UserConsentVerifier.CheckAvailabilityAsync().GetAwaiter().GetResult();
            return availability == UserConsentVerifierAvailability.Available;
        }

        /// <summary>
        /// Attempts to authenticate the user using Windows Hello.
        /// </summary>
        /// <returns> bool: User authenticated </returns>
        public bool Authenticate()
        {
            var result = UserConsentVerifier.RequestVerificationAsync("Authenticate to unlock App").GetAwaiter().GetResult();
            return result == UserConsentVerificationResult.Verified;
        }


    }
}
