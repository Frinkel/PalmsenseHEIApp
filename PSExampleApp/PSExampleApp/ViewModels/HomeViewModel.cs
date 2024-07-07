using System;
using MvvmHelpers;
using PSExampleApp.Common.Models;
using PSExampleApp.Core.Services;
using PSExampleApp.Forms.Navigation;
using PSExampleApp.Forms.Resx;
using PSExampleApp.Forms.Views;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Services;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Forms;

namespace PSExampleApp.Forms.ViewModels
{
    public class HomeViewModel : BaseAppViewModel
    {
        private readonly IMeasurementService _measurementService;
        private readonly IPopupNavigation _popupNavigation;
        private readonly IUserService _userService;

        public HomeViewModel(IUserService userService, IMeasurementService measurementService, IAppConfigurationService appConfigurationService) : base(appConfigurationService)
        {
            _popupNavigation = PopupNavigation.Instance;

            OnPageAppearingCommand = CommandFactory.Create(OnPageAppearing);
            OpenMeasurementListCommand = CommandFactory.Create(OpenMeasurementList);
            OpenLoginPopupCommand = CommandFactory.Create(OpenLoginPopup);
            OpenHeiCommand = CommandFactory.Create(OpenHeiView);
            OpenMockDataCommand = CommandFactory.Create(OpenMockDataView);
            StartMeasurementCommand = CommandFactory.Create(StartMeasurement);
            ConfigureApplicationCommand = CommandFactory.Create(ConfigureApplication);
            OpenQRScannerCommand = CommandFactory.Create(OpenQRScanner);
            OpenMeasurementHistoryPlotCommand = CommandFactory.Create(OpenMeasurementHistoryPlot);
            _measurementService = measurementService;
            _userService = userService;
            MessagingCenter.Subscribe<object>(this, "UpdateSettings", (_) => { OnPropertyChanged(nameof(ActiveUserIsAdmin)); });
        }

        public bool ActiveUserIsAdmin => _userService.ActiveUser?.IsAdmin ?? false;

        public ICommand ConfigureApplicationCommand { get; set; }
        public ICommand OnPageAppearingCommand { get; set; }
        public ICommand OnPageDisappearingCommand { get; set; }
        public ICommand OpenLoginPopupCommand { get; }
        public ICommand OpenHeiCommand { get; }
        public ICommand OpenMockDataCommand { get; }
        public ICommand OpenMeasurementListCommand { get; }
        public ICommand StartMeasurementCommand { get; }
        public ICommand OpenQRScannerCommand { get; }
        public ICommand OpenMeasurementHistoryPlotCommand { get; }

        public async Task OnPageAppearing()
        {
            await _measurementService.InitializeMeasurementConfigurations();
            _userService.ActiveUserChanged += _userService_ActiveUserChanged;

            if (_userService.ActiveUser == null)
            {
                //This is during the initialization of the project. We check if the popup stack is 0. If its not then it means that the page onappearing is not triggered by the app startup
                if (_popupNavigation.PopupStack.Count == 0)
                {
                    await NavigationDispatcher.Push(NavigationViewType.LoginView);

                    await _appConfigurationService.InitializeMethod();
                }
            }

            if (_userService?.ActiveUser?.UserLinearEquationConfiguration.Slope == 0.0 ||
                _userService?.ActiveUser?.UserLinearEquationConfiguration.Intercept == 0.0)
            {
                var missingSensorInformationAlert = await NavigationDispatcher.PushAlert("Missing Sensor information", "Currently no Sensor information is configured, please scan an information QR code to configure the sensor. \n\n Press 'OK' to get redirected to the configuration page.");
                if (missingSensorInformationAlert)
                {
                    await NavigationDispatcher.Push(NavigationViewType.QRCodeScannerPage);
                }
            }
            else if (_userService?.ActiveUser?.UserLinearEquationConfiguration.SensorExpirationDate < DateTime.Now)
            {
                var confirmExpirationDateAlert = await NavigationDispatcher.PushAlert("Sensor expired!", "The currently configured Sensor has expired. Confirm if you wish to keep using it, otherwise you need to configure a new Sensor.");
                if (!confirmExpirationDateAlert)
                {
                    await NavigationDispatcher.Push(NavigationViewType.QRCodeScannerPage);
                }
            }


            MessagingCenter.Send<object>(this, "DiscoverDevices");
        }

        public void OnPageDisappearing()
        {
            _userService.ActiveUserChanged -= _userService_ActiveUserChanged;
        }

        private void _userService_ActiveUserChanged(object sender, Common.Models.User e)
        {
            OnPropertyChanged(nameof(ActiveUserIsAdmin));
        }

        private async Task ConfigureApplication()
        {
            await NavigationDispatcher.Push(NavigationViewType.ConfigureApplicationView);
        }

        private async Task OpenLoginPopup()
        {
            await NavigationDispatcher.Push(NavigationViewType.LoginView);
        }

        private async Task OpenMeasurementList()
        {
            await NavigationDispatcher.Push(NavigationViewType.SelectMeasurementView);
        }

        private async Task StartMeasurement()
        {
            bool useMockData = _userService.ActiveUser.UseMockData;

            if (useMockData)
            {
                await NavigationDispatcher.Push(NavigationViewType.MockDataView);
            }
            else
            {
                await NavigationDispatcher.Push(NavigationViewType.SelectDeviceView);
            }
            
        }

        private async Task OpenHeiView()
        {
            await NavigationDispatcher.Push(NavigationViewType.HeiView);
        }

        private async Task OpenMockDataView()
        {
            await NavigationDispatcher.Push(NavigationViewType.MockDataView);
        }

        private async Task OpenQRScanner()
        {
            await NavigationDispatcher.Push(NavigationViewType.QRCodeScannerPage);
        }

        private async Task OpenMeasurementHistoryPlot()
        {
            await NavigationDispatcher.Push(NavigationViewType.MeasurementHistoryPlotView);
        }
    }
}