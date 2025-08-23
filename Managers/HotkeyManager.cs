using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using AppLock.Models;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using AppLock.Utils;
namespace AppLock.Managers
{
    public class HotkeyManager : IDisposable
    {
        #region Win32 APIs


        // pointer to window handle, the id of the hotkey, modifier keys, virtual key code
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);


        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Windows message id for hotkey
        private const int WM_HOTKEY = 0x0312;

        #endregion


        private readonly Dictionary<int, HotkeyRegistration> _registeredHotkeys = new Dictionary<int, HotkeyRegistration>();
        private readonly Window _parentWindow;
        private readonly HwndSource _hwndSource;

        private int _nextHotkeyId= 1; // unique id for each hotkey
        private bool _disposed = false;

        public event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;
        public event EventHandler<string> HotkeyError;

        public HotkeyManager(Window parentWindow)
        {
            _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));

            // get window handle for the win32 interlop
            _hwndSource = (HwndSource)HwndSource.FromVisual(_parentWindow);
            if (_hwndSource == null )
            {
                throw new InvalidOperationException("Could not get window handle for hotkey registration");
            }

            // hook for window msg intercepts
            _hwndSource.AddHook(WndProc);
            RegisterDefaultHotkeys();
        }

        /// <summary>
        /// Registers teh default hotkeys, Ctrl + L
        /// </summary>
        private void RegisterDefaultHotkeys()
        {
            RegisterHotkey(AppLock.Models.ModifierKeys.Control, Key.L, HotkeyAction.ToggleLockCurrentApp, "Toggle Lock Current App");
        }


        /// <summary>
        /// Registers a Hotkey, check if HotKeyManager is not disposed and then call the WIN32 API to register
        /// </summary>
        /// <param name="modifierKeys"></param>
        /// <param name="key"></param>
        /// <param name="action"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public int RegisterHotkey(AppLock.Models.ModifierKeys modifierKeys, Key key, HotkeyAction action, string description=null)
        {
            if (_disposed)
            {
                HotkeyError?.Invoke(this, "HotkeyManager has been disposed");
                return -1;
            }
            int hotkeyID = _nextHotkeyId++;
            try
            {
                // convert wpf key to vitrual key code
                uint virtualKeyCode = (uint)KeyInterop.VirtualKeyFromKey(key);
                bool success = RegisterHotKey(_hwndSource.Handle, hotkeyID, (uint)modifierKeys, virtualKeyCode);
                if ( success)
                {
                    var registration = new HotkeyRegistration
                    {
                        ID = hotkeyID,
                        Modifiers = modifierKeys,
                        Key = key,
                        Action = action,
                        Description = description,
                        RegistrationTime = DateTime.Now
                    };
                    _registeredHotkeys[hotkeyID] = registration;
                    return hotkeyID;
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    string errorMessage = $"Failed to register hotkey {modifierKeys}+{key}. Error code: {error}";
                    if (error == 1409)
                    {
                        errorMessage += " (Hotkey already in use by another application)";
                    }
                    HotkeyError?.Invoke(this, errorMessage);
                    return -1;
                }
            }
            catch (Exception ex)
            {
                HotkeyError?.Invoke(this, $"Exception registering hotkey {modifierKeys}+{key}: {ex.Message}");
                return -1;
            }
        }



        /// <summary>
        /// Deregisters hotkey
        /// </summary>
        /// <param name="hotkeyID"></param>
        /// <returns></returns>
        public bool UnregisterHotkey(int hotkeyID)
        {
            if (_disposed || !_registeredHotkeys.ContainsKey(hotkeyID))
            {
                return false;
            }

            try
            {
                bool success = UnregisterHotKey(_hwndSource.Handle, hotkeyID);
                if (success)
                {
                    _registeredHotkeys.Remove(hotkeyID);
                }
                return success;
            }
            catch (Exception ex)
            {
                HotkeyError?.Invoke(this, $"Exception unregistering hotkey {hotkeyID}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets infor about a registered hotkey
        /// </summary>
        /// <param name="hotkeyID"></param>
        /// <returns></returns>
        public HotkeyRegistration GetHotkeyInfo(int hotkeyID)
        {
            _registeredHotkeys.TryGetValue(hotkeyID, out var registration);
            return registration;
        }

        public IReadOnlyCollection<HotkeyRegistration> GetAllHotkeys()
        {
            return _registeredHotkeys.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Check if a hotkey is registed
        /// </summary>
        /// <param name="modifierKeys"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsHotkeyRegistered(AppLock.Models.ModifierKeys modifierKeys, Key key)
        {
            return _registeredHotkeys.Values.Any(hk => hk.Modifiers == modifierKeys && hk.Key == key);
        }

        /// <summary>
        /// Processes Windows messages sent to the window, including handling registered hotkey events.
        /// </summary>
        /// <remarks>This method listens for the <see langword="WM_HOTKEY"/> message to detect when a
        /// registered hotkey is pressed. If a hotkey is recognized, the <see cref="HotkeyPressed"/> event is raised
        /// with the corresponding event arguments.
        /// IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        /// </remarks>
        /// <param name="hwnd">A handle to the window receiving the message.</param>
        /// <param name="msg">The message identifier.</param>
        /// <param name="wParam">Additional message-specific information, typically used to identify the hotkey.</param>
        /// <param name="lParam">Additional message-specific information, typically unused in this implementation.</param>
        /// <param name="handled">A value indicating whether the message was handled. Set to <see langword="true"/> if the message is
        /// processed; otherwise, <see langword="false"/>.</param>
        /// <returns>An <see cref="IntPtr"/> representing the result of the message processing. Returns <see cref="IntPtr.Zero"/>
        /// in this implementation.</returns>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int hotkeyID = wParam.ToInt32();

                if (_registeredHotkeys.TryGetValue(hotkeyID, out var registration))
                {
                    var eventArgs = new HotkeyPressedEventArgs(registration);
                    HotkeyPressed?.Invoke(this, eventArgs);
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Updates description of hotkey
        /// </summary>
        /// <param name="hotkeyID"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public bool UpdateHotkeyDescription(int hotkeyID, string description)
        {
            if (_registeredHotkeys.TryGetValue((int)hotkeyID, out var registration))
            {
                registration.Description = description;
                return true;
            }
            return false;
        }


        #region Diposeable Implementaiton
        public void Dispose() {
            if (_disposed)
            {
                return;
            }

            var hotkeyIds = _registeredHotkeys.Keys.ToList();
            foreach (int hotkeyID in hotkeyIds)
            {
                UnregisterHotkey( hotkeyID);
            }
        }
        #endregion
    }
}
