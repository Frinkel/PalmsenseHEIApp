using MvvmHelpers;
using Newtonsoft.Json;
using PalmSens.Core.Simplified.XF.Application.Services;
using PSExampleApp.Common.Models;
using PSExampleApp.Core.Services;
using System.Diagnostics;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Forms;
using ZXing.Mobile;
using PalmSens.Comm;

namespace PSExampleApp.Forms.ViewModels
{
    public class SettingsViewModel : BaseAppViewModel
    {
        private readonly IUserService _userService;
        private bool _isAdmin;
        private bool _useMockData;
        private Language _language;
        private bool _settingsChanged;
        private readonly IMessageService _messageService;

        public SettingsViewModel(IUserService userService, IAppConfigurationService appConfigurationService, IMessageService messageService) : base(appConfigurationService)
        {
            _userService = userService;
            _settingsChanged = false;
            _messageService = messageService;
            if (_userService?.ActiveUser != null)
            {
                IsAdmin = _userService.ActiveUser.IsAdmin;
                UseMockData = _userService.ActiveUser.UseMockData;
            }

            OnPageDisappearingCommand = CommandFactory.Create(OnDisappearing);

            //ScanCommand = CommandFactory.Create(ScanAsync);
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                _isAdmin = value;
                _userService.ActiveUser.IsAdmin = value;
                _settingsChanged = true;
            }
        }

        public bool UseMockData
        {
            get => _useMockData;
            set
            {
                _useMockData = value;
                _userService.ActiveUser.UseMockData = value;
                _settingsChanged = true;
            }
        }

        public ICommand ScanCommand { get; }
        public ICommand OnPageDisappearingCommand { get; }

        private void OnDisappearing()
        {
            //Don't do anything if the user setting is changed
            if (!_settingsChanged || _userService?.ActiveUser is null)
                return;

            MessagingCenter.Send<object>(this, "UpdateSettings");
        }
    }
}