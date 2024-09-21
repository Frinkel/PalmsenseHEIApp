using PalmSens.Core.Simplified.XF.Application.Services;
using PSExampleApp.Common.Models;
using PSExampleApp.Core.Services;
using PSExampleApp.Forms.Navigation;
using PSExampleApp.Forms.Resx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;

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
        private string _targetFrequency;

        public SettingsViewModel(IUserService userService, IAppConfigurationService appConfigurationService, IMessageService messageService) : base(appConfigurationService)
        {
            _userService = userService;
            _settingsChanged = false;
            _messageService = messageService;
            if (_userService?.ActiveUser != null)
            {
                IsAdmin = _userService.ActiveUser.IsAdmin;
                UseMockData = _userService.ActiveUser.UseMockData;
                TargetFrequency = _userService.ActiveUser.TargetFrequency.ToString();
            }

            OnPageDisappearingCommand = CommandFactory.Create(OnDisappearing);
            TargetFrequencyCommand = CommandFactory.Create(OnChangeTargetFrequency);
            ConfigureMethodCommand = CommandFactory.Create(ConfigureMethod);

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

        public ICommand OnPageDisappearingCommand { get; }
        public ICommand TargetFrequencyCommand { get; }
        public ICommand ConfigureMethodCommand { get; }
        public string TargetFrequency
        {
            get => _targetFrequency;
            set
            {
                _targetFrequency = value;
                OnPropertyChanged();
            }
        }

        private void OnDisappearing()
        {
            //Don't do anything if the user setting is changed
            if (!_settingsChanged || _userService?.ActiveUser is null)
                return;

            MessagingCenter.Send<object>(this, "UpdateSettings");
        }


        private async void OnChangeTargetFrequency()
        {
            double TargetFrequencyValue;
            if (double.TryParse(TargetFrequency, out TargetFrequencyValue))
            {
                _userService.ActiveUser.TargetFrequency = TargetFrequencyValue;
                await _userService.UpdateUser();

                _messageService.ShortAlert($"Target frequency {TargetFrequencyValue} configured successfully.");
            }
            else
            {
                _messageService.ShortAlert("Invalid input for target frequency.");
                return;
            }
        }


        private async Task ConfigureMethod()
        {
            var customFileType =
                new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                { DevicePlatform.iOS, new[] { "application/octet-stream" } },
                { DevicePlatform.Android, new[] { "application/octet-stream" } },
                });
            var options = new PickOptions
            {
                PickerTitle = AppResources.Picker_SelectMethodFile,
                FileTypes = customFileType,
            };

            try
            {
                FileResult result = null;
                if (Device.RuntimePlatform == Device.iOS)
                {
                    result = await FilePicker.PickAsync();
                }
                else if (Device.RuntimePlatform == Device.Android)
                {
                    result = await FilePicker.PickAsync(options);
                }

                if (result != null)
                {
                    if (!result.FileName.EndsWith("psmethod"))
                    {
                        _messageService.ShortAlert(AppResources.Alert_SelectValidMethodFile);
                        return;
                    }

                    using var stream = await result.OpenReadAsync();

                    await _appConfigurationService.SaveConfigurationMethod(stream);
                    _messageService.ShortAlert(AppResources.Alert_MethodSaved);
                }
            }
            catch (PermissionException)
            {
                _messageService.LongAlert(AppResources.Alert_FailedImport);
            }
            catch (Exception)
            {
                _messageService.LongAlert(AppResources.Alert_FailedImportMethod);
            }
        }
    }
}