using PalmSens.Comm;
using PalmSens.Core.Simplified.XF.Application.Services;
using PalmSens.Plottables;
using PSExampleApp.Common.Models;
using PSExampleApp.Core.Services;
using PSExampleApp.Forms.Navigation;
using PSExampleApp.Forms.Resx;
using PSExampleApp.Forms.Views;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing.Mobile;
using System.Globalization;

namespace PSExampleApp.Forms.ViewModels
{
    public class QRCodeScannerViewModel : BaseAppViewModel
    {
        private readonly IMeasurementService _measurementService;
        private readonly IMessageService _messageService;
        private readonly IPopupNavigation _popupNavigation;
        private readonly IShareService _shareService;
        private readonly IUserService _userService;
        private string _scannedResult;

        public QRCodeScannerViewModel(
            IMeasurementService measurementService,
            IShareService shareService,
            IAppConfigurationService appConfigurationService,
            IMessageService messageService,
            IUserService userService)
            : base(appConfigurationService)
        {
            _measurementService = measurementService;
            _messageService = messageService;
            _shareService = shareService;
            _popupNavigation = PopupNavigation.Instance;
            _userService = userService;
            ScanCommand = CommandFactory.Create(ScanAsync);
            NavigateToHomeCommand = CommandFactory.Create(NavigateToHome);
        }

        public string ScannedResult
        {
            get => _scannedResult;
            set
            {
                if (_scannedResult != value)
                {
                    _scannedResult = value;
                    OnPropertyChanged(nameof(ScannedResult));
                }
            }
        }

        public ICommand ScanCommand { get; }
        public ICommand NavigateToHomeCommand { get; }

        private LinearEqConfiguration _linearEqConfiguration
        {
            get
            {
                return _userService.ActiveUser?.UserLinearEquationConfiguration;
            }
        }

        public string ViewFriendlyLinearEquation
        {
            get
            {
                return !(_linearEqConfiguration.Intercept == 0.0 || _linearEqConfiguration.Slope == 0.0)
                    ? $"Linear Equation: y = {_linearEqConfiguration.Intercept} + {_linearEqConfiguration.Slope} * x"
                    : "No Linear Eq Configured.";
            }
        }

        public string BatchNumber
        {
            get
            {
                return _linearEqConfiguration.BatchNumber != -1
                    ? $"Batch Number: {_linearEqConfiguration.BatchNumber}"
                    : "Missing Batch Number.";
            }
        }

        public string ExpirationDate
        {
            get
            {
                return !_linearEqConfiguration.SensorExpirationDate.Equals(DateTime.MinValue)
                    ? $"Expiration Date: {_linearEqConfiguration.SensorExpirationDate.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)}"
                    : "No Expiration Date Configured.";
            }
        }


        private async Task ScanAsync()
        {
            try
            {
                var scanner = new MobileBarcodeScanner();

                var result = await scanner.Scan();

                if (result != null)
                {
                    try
                    {
                        var configuration = JsonConvert.DeserializeObject<LinearEqConfiguration>(result.Text);

                        if (configuration.Slope == null || configuration.Intercept == null)
                        {
                            _messageService.ShortAlert("Failed to parse configuration.");
                        }
                        else if (configuration != null)
                        {
                            _messageService.ShortAlert("Scanned QR Code and parsed configuration.");

                            _userService.ActiveUser.UserLinearEquationConfiguration = configuration;
                            OnPropertyChanged(nameof(ViewFriendlyLinearEquation));
                            OnPropertyChanged(nameof(BatchNumber));
                            OnPropertyChanged(nameof(ExpirationDate));
                            await _userService.UpdateUser();
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        Debug.WriteLine($"JSON Error: {jsonEx.Message}");
                        _messageService.ShortAlert("Error: Failed to parse JSON data.");
                    }
                }
                else
                {
                    _messageService.ShortAlert("No QR code detected.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scanning QR code: {ex.Message}");
                _messageService.ShortAlert("Error: Failed to scan QR code. Please try again.");
            }
        }
    

        private async Task NavigateToHome()
        {
            _measurementService.ResetMeasurement();
            await NavigationDispatcher.PopToRoot();
        }
    }
}
