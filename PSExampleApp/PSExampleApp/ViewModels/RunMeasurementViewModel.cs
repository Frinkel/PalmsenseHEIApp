using MvvmHelpers;
using PalmSens;
using PalmSens.Core.Simplified.Data;
using PalmSens.Core.Simplified.XF.Application.Services;
using PalmSens.Devices;
using PSExampleApp.Common.Models;
using PSExampleApp.Core.Extentions;
using PSExampleApp.Core.Services;
using PSExampleApp.Forms.Navigation;
using PSExampleApp.Forms.Resx;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Newtonsoft.Json.Linq;


namespace PSExampleApp.Forms.ViewModels
{
    public class RunMeasurementViewModel : BaseAppViewModel
    {
        private ILoadSavePlatformService _loadSavePlatformService;
        private readonly IDeviceService _deviceService;
        private readonly IMeasurementService _measurementService;
        private readonly IMessageService _messageService;
        private SimpleCurve _activeCurve;
        private Countdown _countdown = new Countdown();

        private bool _measurementFinished = false;
        private double _progress;
        private int _progressPercentage;

        //private bool UseMockData = false;
        public bool UseMockData { get; set; } = true;
        public int DataReceivedCount { get; set; } = 0;

        public RunMeasurementViewModel(IMeasurementService measurementService, IMessageService messageService, IDeviceService deviceService, IAppConfigurationService appConfigurationService, ILoadSavePlatformService loadSavePlatformService) : base(appConfigurationService)
        {
            _loadSavePlatformService = loadSavePlatformService;
            Progress = 0;
            _deviceService = deviceService;
            _messageService = messageService;
            _measurementService = measurementService;
            _measurementService.DataReceived += _measurementService_DataReceived;
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

        private void _measurementService_DataReceived(object sender, SimpleCurve activeSimpleCurve)
        {
            DataReceivedCount++;
            _activeCurve = activeSimpleCurve;
            if (_activeCurve != null) // Added null check
            {
                _activeCurve.NewDataAdded += ActiveSimpleCurve_NewDataAdded;
            }
            else
            {
                Debug.WriteLine("Error: _activeCurve is null in _measurementService_DataReceived."); // Added logging
            }
        }


        //private async void _measurementService_MeasurementEnded(object sender, EventArgs e)
        //{
        //    Debug.WriteLine($"Data received {DataReceivedCount} times");


        //    if(UseMockData)
        //    {
        //        _activeCurve.WipeDataPoints();
        //        await LoadAndProcessMockDataAsync();
        //    }
        //    else
        //    {
        //        _activeCurve.NewDataAdded -= ActiveSimpleCurve_NewDataAdded;
        //        _countdown.Ticked -= OnCountdownTicked;
        //    }

        //    RunPeakAnalysis().WithCallback();
        //}

        private async void _measurementService_MeasurementEnded(object sender, EventArgs e)
        {
            try // Added try-catch block
            {
                Debug.WriteLine($"Data received {DataReceivedCount} times");

                if (_activeCurve == null) // Added null check
                {
                    Debug.WriteLine("Error: _activeCurve is null in _measurementService_MeasurementEnded.");
                    return;
                }

                if (UseMockData)
                {
                    _activeCurve.WipeDataPoints();
                    await LoadAndProcessMockDataAsync();
                }
                else
                {

                    _activeCurve.NewDataAdded -= ActiveSimpleCurve_NewDataAdded;


                    if (_countdown != null) // Added null check
                    {
                        _countdown.Ticked -= OnCountdownTicked;
                    }
                }

                RunPeakAnalysis().WithCallback();
            }
            catch (Exception ex) // Added exception handling
            {
                Debug.WriteLine($"An error occurred in _measurementService_MeasurementEnded: {ex.Message}");
            }
        }


        private async void ActiveSimpleCurve_NewDataAdded(object sender, PalmSens.Data.ArrayDataAddedEventArgs e)
        {

            // Process actual measurement data
            int startIndex = e.StartIndex;
            int count = e.Count;

            for (int i = startIndex; i < startIndex + count; i++)
            {
                double xValue = _activeCurve.XAxisValue(i);
                double yValue = _activeCurve.YAxisValue(i);

                Debug.WriteLine($"Data received potential {xValue}, current {yValue}");
                ReceivedData.Add($"potential {xValue}, current {yValue}");
            }

        }

        private async Task Continue()
        {
            //The continue will trigger the save of the measurement. //TODO maybe add cancel in case user doesn't want to save
            ActiveMeasurement.MeasurementDate = DateTime.Now.Date;
            await _measurementService.SaveMeasurement(ActiveMeasurement);
            await NavigationDispatcher.Push(NavigationViewType.MeasurementFinishedView);
        }

        private void Curve_DetectedPeaks(object sender, EventArgs e)
        {
            _measurementService.CalculateConcentration();

            //After the concentration is calculated we allow the user to press continue
            Progress = 1;
            ProgressPercentage = 100;
            MeasurementIsFinished = true;
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

        private async Task RunPeakAnalysis()
        {
            _activeCurve.DetectedPeaks += Curve_DetectedPeaks;
            //NOTE: When running a LSV or CV a 'PeakTypes.LSVCV' is more appropriate.
            await _activeCurve.DetectPeaksAsync(
                ActiveMeasurement.Configuration.ConcentrationMethod.PeakMinWidth,
                ActiveMeasurement.Configuration.ConcentrationMethod.PeakMinHeight,
                true,
                PeakTypes.Default);
        }



        private async Task LoadAndProcessMockDataAsync()
        {
            try
            {
                SimpleMeasurement mockMeasurement = await _loadSavePlatformService.LoadMeasurementFromAssetAsync("EIS_HEI_2.pssession");
                var t1 = mockMeasurement.SimpleCurveCollection[0];
                if (mockMeasurement != null && mockMeasurement.SimpleCurveCollection != null)
                {

                    foreach (var curve in mockMeasurement.SimpleCurveCollection)
                    {
                        _activeCurve.AddCustom(curve);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading mock data: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}"); // Provides the call stack
                if (ex.InnerException != null)
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}"); // Additional details if there is an inner exception
            }
        }
    }
}