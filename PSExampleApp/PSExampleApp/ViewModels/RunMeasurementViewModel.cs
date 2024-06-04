using PalmSens;
using PalmSens.Core.Simplified.XF.Application.Services;
using PSExampleApp.Common.Models;
using PSExampleApp.Core.Services;
using PSExampleApp.Forms.Navigation;
using PSExampleApp.Forms.Resx;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;

namespace PSExampleApp.Forms.ViewModels
{
    public class RunMeasurementViewModel : BaseAppViewModel
    {
        private readonly IDeviceService _deviceService;
        private readonly IMeasurementService _measurementService;
        private readonly IMessageService _messageService;
        private Countdown _countdown = new Countdown();

        private bool _measurementFinished = false;
        private double _progress;
        private int _progressPercentage;

        public RunMeasurementViewModel(IMeasurementService measurementService, IMessageService messageService, IDeviceService deviceService, IAppConfigurationService appConfigurationService) : base(appConfigurationService)
        {
            Progress = 0;
            _deviceService = deviceService;
            _messageService = messageService;
            _measurementService = measurementService;
            _measurementService.MeasurementStarted += _measurementService_MeasurementStarted;
            _measurementService.MeasurementEnded += _measurementService_MeasurementEnded;

            ActiveMeasurement = _measurementService.ActiveMeasurement;

            OnPageAppearingCommand = CommandFactory.Create(OnPageAppearing, onException: ex =>
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                //DisplayAlert();
                                Console.WriteLine(ex.Message);
                            }), allowsMultipleExecutions: false);

            ContinueCommand = CommandFactory.Create(Continue);
        }

        public HeavyMetalMeasurement ActiveMeasurement { get; }

        public ICommand ContinueCommand { get; }

        public bool MeasurementIsFinished
        {
            get => _measurementFinished;
            set => SetProperty(ref _measurementFinished, value);
        }

        public ICommand OnPageAppearingCommand { get; }

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public int ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public ObservableCollection<string> ReceivedData { get; set; } = new ObservableCollection<string>();

        private void _measurementService_MeasurementStarted(object sender, EventArgs e)
        {
            _messageService.ShortAlert("Measurement started");
        }

        private void _measurementService_MeasurementEnded(object sender, EventArgs e)
        {
            _messageService.ShortAlert("Measurement ended");
            _countdown.Ticked -= OnCountdownTicked;

            double targetFrequency = 126.0;
            _measurementService.HeiCalculateConcentration(targetFrequency);

            Progress = 1;
            ProgressPercentage = 100;
            MeasurementIsFinished = true;
        }

        private async Task Continue()
        {
            //The continue will trigger the save of the measurement. //TODO maybe add cancel in case user doesn't want to save
            ActiveMeasurement.MeasurementDate = DateTime.Now.Date;
            await _measurementService.SaveMeasurement(ActiveMeasurement);
            await NavigationDispatcher.Push(NavigationViewType.HeiView);
        }

        private async Task<Method> LoadDiffPulseMethod()
        {
            try
            {
                return await _appConfigurationService.LoadConfigurationMethod();
            }
            catch (Exception)
            {
                // When the method file cannot be found it means that it's manually removed. In this case the app needs to be reinstalled
                MainThread.BeginInvokeOnMainThread(() => _messageService.ShortAlert(AppResources.Alert_MethodNotFound));
                throw;
            }
        }

        private void OnCountdownTicked()
        {
            Progress = _countdown.ElapsedTime / _countdown.TotalTimeInMilliSeconds;
            ProgressPercentage = (int)(Progress * 100);
        }

        private async Task OnPageAppearing()
        {
            var method = await LoadDiffPulseMethod();

            try
            {
                _countdown.Start((int)Math.Round(method.GetMinimumEstimatedMeasurementDuration(_measurementService.Capabilities) * 1000));
                _countdown.Ticked += OnCountdownTicked;

                _measurementService.ActiveMeasurement.Measurement = await _measurementService.StartMeasurement(method);
            }
            catch (NullReferenceException)
            {
                // Nullreference is thrown when device is not connected anymore. In this case we pop back to homescreen. The user can then try to reconnect again
                _messageService.ShortAlert(AppResources.Alert_NotConnected);
                this._measurementService.ResetMeasurement();
                await _deviceService.DisconnectDevice();
                await NavigationDispatcher.PopToRoot();
            }
            catch (ArgumentException)
            {
                // Argument exception is thrown when method is incompatible with the connected device.
                _messageService.ShortAlert(AppResources.Alert_DeviceIncompatible);
                this._measurementService.ResetMeasurement();
                await _deviceService.DisconnectDevice();
                await NavigationDispatcher.PopToRoot();
            }
            catch (Exception ex)
            {
                _messageService.LongAlert(AppResources.Alert_SomethingWrong);
                Debug.WriteLine(ex);
                this._measurementService.ResetMeasurement();
                await _deviceService.DisconnectDevice();
                await NavigationDispatcher.PopToRoot();
            }
        }
    }
}