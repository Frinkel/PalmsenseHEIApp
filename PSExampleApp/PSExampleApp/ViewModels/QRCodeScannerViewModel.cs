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

namespace PSExampleApp.Forms.ViewModels
{
    public class QRCodeScannerViewModel : BaseAppViewModel
    {
        private readonly IMeasurementService _measurementService;
        private readonly IMessageService _messageService;
        private readonly IPopupNavigation _popupNavigation;
        private readonly IShareService _shareService;
        private string _scannedResult;

        public QRCodeScannerViewModel(
            IMeasurementService measurementService,
            IShareService shareService,
            IAppConfigurationService appConfigurationService,
            IMessageService messageService)
            : base(appConfigurationService)
        {
            _measurementService = measurementService;
            _messageService = messageService;
            _shareService = shareService;
            _popupNavigation = PopupNavigation.Instance;
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

        private async Task ScanAsync()
        {
            try
            {
                var scanner = new MobileBarcodeScanner();
                if (scanner == null)
                {
                    Debug.WriteLine("Scanner is not initialized");
                    _messageService.ShortAlert("Scanner initialization failed.");
                    return;
                }

                var result = await scanner.Scan();

                if (result != null)
                {
                    ScannedResult = result.Text;

                    LinearEqConfiguration linearEqConfiguration = JsonConvert.DeserializeObject<LinearEqConfiguration>(ScannedResult);
                    _messageService.ShortAlert("Scanned QR Code");
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
