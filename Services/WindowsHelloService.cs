using AppLock.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials.UI;
using System.Security.Principal;

namespace AppLock.Services
{
    public class WindowsHelloService : IAuthService
    {
        public string Name => "Windows Hello";

        /// <summary>
        /// Checks if Windows Hello is available for authentication on device.
        /// </summary>
        /// <returns> bool </returns>
        public async Task<bool> IsAvailableAsync()
        {
            var availability = await UserConsentVerifier.CheckAvailabilityAsync();
            return availability == UserConsentVerifierAvailability.Available;
        }

        /// <summary>
        /// Attempts to authenticate the user using Windows Hello.
        /// </summary>
        /// <returns> bool: User authenticated </returns>
        public async Task<bool> AuthenticateAsync()
        {
            var result = await UserConsentVerifier.RequestVerificationAsync("Authenticate to unlock App");
            return result == UserConsentVerificationResult.Verified;
        }

        public bool IsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

    }
}
