using PalmSens.Comm;
using PalmSens.Core.Simplified.Data;
using PalmSens.Core.Simplified.XF.Application.Services;
using PalmSens.Plottables;
using PSExampleApp.Common.Models;
using PSExampleApp.Core.Services;
using PSExampleApp.Forms.Navigation;
using PSExampleApp.Forms.Resx;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;

namespace PSExampleApp.Forms.ViewModels
{
    public class MockDataViewModel : BaseAppViewModel
    {
        private readonly ILoadSavePlatformService _loadSavePlatformService;
        private readonly IUserService _userService;
        private readonly IMessageService _messageService;
        private readonly IPopupNavigation _popupNavigation;
        private readonly IMeasurementService _measurementService;
        private SimpleCurve _activeCurve;
        private bool _measurementFinished = false;
        private double _progress;
        private int _progressPercentage;

        public MockDataViewModel(
            IMeasurementService measurementService,
            IAppConfigurationService appConfigurationService,
            IUserService userService,
            ILoadSavePlatformService loadSavePlatformService,
            IMessageService messageService) : base(appConfigurationService)
        {
            _userService = userService;
            _loadSavePlatformService = loadSavePlatformService;
            _messageService = messageService;
            _popupNavigation = PopupNavigation.Instance;
            _measurementService = measurementService;

            ActiveUser = _userService.ActiveUser;
            LoadMockDataCommand = CommandFactory.Create(LoadMockData);
            OnPageAppearingCommand = CommandFactory.Create(OnPageAppearing);
            ContinueCommand = CommandFactory.Create(Continue);

        }

        public ICommand LoadMockDataCommand { get; }
        public ICommand OnPageAppearingCommand { get; }
        public ICommand ContinueCommand { get; }


        private async Task OnPageAppearing()
        {
            await LoadMockData();
            await Continue();
        }

        public User ActiveUser { get; set; }


        public HeavyMetalMeasurement ActiveMeasurement { get; set; }


        public bool MeasurementIsFinished
        {
            get => _measurementFinished;
            set => SetProperty(ref _measurementFinished, value);
        }

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

        private async Task LoadMockData()
        {
            try
            {
                SimpleMeasurement mockMeasurement = await _loadSavePlatformService.LoadMeasurementFromAssetAsync("EIS_HEI_2.pssession");

                ActiveMeasurement = MockHeavyMetalMeasurement(mockMeasurement);
                
                if (mockMeasurement != null && mockMeasurement.SimpleCurveCollection != null)
                {
                    foreach (var curve in mockMeasurement.SimpleCurveCollection)
                    {
                        if (_activeCurve == null)
                        {
                            _activeCurve = curve;
                        }
                        else
                        {
                            _activeCurve.AddCustom(curve);
                        }
                    }
                    ProcessMockData(_activeCurve);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading mock data: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
        }

        private async Task Continue()
        {
            //The continue will trigger the save of the measurement. //TODO maybe add cancel in case user doesn't want to save
            ActiveMeasurement.MeasurementDate = DateTime.Now.Date;
            await _measurementService.SaveMeasurement(ActiveMeasurement);
            await NavigationDispatcher.Push(NavigationViewType.MeasurementFinishedView);
        }

        private HeavyMetalMeasurement MockHeavyMetalMeasurement(SimpleMeasurement mockMeasurement)
        {

            var uniqueName = GenerateUniqueName("Mock Experiment");

            var someMeasurement= new HeavyMetalMeasurement
            {
                Name = uniqueName,
                Id = Guid.NewGuid(),
                Concentration = 0.0, // Set a mock concentration value
                Configuration = new MeasurementConfiguration(), // Initialize with a default or mock configuration
                Measurement = mockMeasurement,
                MeasurementDate = DateTime.Now,
                MeasurementImages = new List<byte[]>()
            };

            return someMeasurement;
        }

        private string GenerateUniqueName(string baseName)
        {
            var existingMeasurements = _userService.ActiveUser.Measurements;
            int counter = 1;
            string newName;
            do
            {
                newName = $"{baseName} {counter}";
                counter++;
            } while (existingMeasurements.Any(x => x.Name == newName));
            return newName;
        }

        private void ProcessMockData(SimpleCurve curve)
        {
            try
            {
                RunPeakAnalysis(curve).ConfigureAwait(false);
                MeasurementIsFinished = true;
                Progress = 1;
                ProgressPercentage = 100;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing mock data: {ex.Message}");
            }
        }

        private async Task RunPeakAnalysis(SimpleCurve curve)
        {
            try
            {
                curve.DetectedPeaks += Curve_DetectedPeaks;
                await curve.DetectPeaksAsync(
                    ActiveMeasurement.Configuration.ConcentrationMethod.PeakMinWidth,
                    ActiveMeasurement.Configuration.ConcentrationMethod.PeakMinHeight,
                    true,
                    PeakTypes.Default);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in peak analysis: {ex.Message}");
            }
        }

        private void Curve_DetectedPeaks(object sender, EventArgs e)
        {
            // Assuming _measurementService.CalculateConcentration() is available and used here
            _measurementService.CalculateConcentration();

            // After the concentration is calculated we allow the user to press continue
            MeasurementIsFinished = true;
            Progress = 1;
            ProgressPercentage = 100;
        }
    }
}
