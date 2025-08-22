using AppLock.Models;
using AppLock.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLock.Managers
{
    public class AuthenticationManager
    {
        private readonly WindowsHelloService _windowsHelloService;
        private readonly AppLockSettings _settings;


        // simple attempt tracking
        private int _failedAttempts = 0;
        private DateTime _lastAttemptTime = DateTime.MinValue;


        // Lockout Values
        public AuthenticationManager(WindowsHelloService windowsHelloService, AppLockSettings settings)
        {
            _windowsHelloService = windowsHelloService;
            _settings = settings;
        }


        /// <summary>
        /// Attempts unlock, increments fail attempts if success is false
        /// </summary>
        /// <returns></returns>
        public async Task<bool> AuthenticateAsync()
        {
            if (IsInLockout())
            {
                return false;
            }

            bool success = false;
            if (_settings.UseWindowsHello)
            {
                success = await TryWindowsHelloAsync();
            }

            if (!success)
            {
                _failedAttempts++;
                _lastAttemptTime = DateTime.Now;
            }
            else
            {
                _failedAttempts = 0;
            }
            return success;
        }



        /// <summary>
        /// Check if Windows Hello is availible before attempting Auth
        /// </summary>
        /// <returns></returns>
        public async Task<bool> TryWindowsHelloAsync() 
        {
            try
            {
                if (!await _windowsHelloService.IsAvailableAsync())
                {
                    return false;
                }
                return await _windowsHelloService.AuthenticateAsync();
            }
            catch 
            {
                return false;
            }
        }


        /// <summary>
        /// Check if failed attempts is less than max and if not
        /// then make its not in lockout duration
        /// </summary>
        /// <returns></returns>
        public bool IsInLockout()
        {
            if (_failedAttempts < _settings.MaxAuthAttempts)
            {
                return false;
            }
            var timeSinceLastFail = DateTime.Now - _lastAttemptTime;
            return timeSinceLastFail < _settings.LockoutDuration;
        }


        /// <summary>
        /// Gets the remaining lockout time
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetRemainingLockoutTime()
        {
            if (!IsInLockout())
            {
                return TimeSpan.Zero;
            }
            var elapsed = DateTime.Now - _lastAttemptTime;
            return _settings.LockoutDuration - elapsed;
        }

        /// <summary>
        /// Admin Overide
        /// </summary>
        public void ResetFailedAttempts()
        {
            _failedAttempts = 0;
            _lastAttemptTime = DateTime.MinValue;
        }


        /// <summary>
        /// Auth Stats Object
        /// </summary>
        /// <returns></returns>
        public AuthenticationStatus GetStatus()
        {
            return new AuthenticationStatus
            {
                IsInLockout = IsInLockout(),
                FailedAttempts = _failedAttempts,
                MaxAttempts = _settings.MaxAuthAttempts,
                RemainingLockoutTime = GetRemainingLockoutTime(),
                WindowsHelloEnabled = _settings.UseWindowsHello,
                WindowsHelloAvailable = _windowsHelloService.IsAvailableAsync().Result,
            };
        }

    }
}
