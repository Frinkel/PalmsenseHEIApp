using PalmSens.Core.Simplified.Data;
using PalmSens.Core.Simplified.XF.Application.Services;
using PalmSens.Data;
using PSExampleApp.Common.Models;
using PSExampleApp.Core.Services;
using PSExampleApp.Forms.Navigation;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Org.BouncyCastle.Crypto.Engines;
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

        private string mockDataFile = "AllDataExperiment273.pssession";

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

        private async Task LoadMockData()
        {
            try
            {
                SimpleMeasurement mockMeasurement = await _loadSavePlatformService.LoadMeasurementFromAssetAsync(mockDataFile);

                ActiveMeasurement = MockHeavyMetalMeasurement(mockMeasurement);

                double targetFrequency = 126.0;
                var impedanceValue = GetValuesFromTargetFreq(targetFrequency);
                double concentration = CalculateConcentration(impedanceValue);
                ActiveMeasurement.HeiConcentration = concentration;

                if (mockMeasurement != null && mockMeasurement.SimpleCurveCollection != null)
                {
                    foreach (var curve in mockMeasurement.SimpleCurveCollection)
                    {
                        if (_activeCurve == null)
                        {
                            _activeCurve = curve;
                            for (int i = 0; i < curve.NDataPoints; i++)
                            {
                                double xValue = _activeCurve.XAxisValue(i); //Get the value on Curve's X-Axis (potential) at the specified index
                                double yValue = _activeCurve.YAxisValue(i); //Get the value on Curve's Y-Axis (current) at the specified index

                                Debug.WriteLine($"Data received potential {xValue}, current {yValue}");
                            }
                        }
                        else
                        {
                            _activeCurve.AddCustom(curve); // <- custom functionality to add data values to a SimpleCurve
                        }
                    }

                    MeasurementIsFinished = true;
                    Progress = 1;
                    ProgressPercentage = 100;
                    //ProcessMockData(_activeCurve);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading mock data: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private double GetValuesFromTargetFreq(double targetFrequency)
        {
            var baseArray = ActiveMeasurement.Measurement.Measurement.EISdata.FirstOrDefault().EISDataSet.GetDataArrays();
            var frequencyArray = baseArray.FirstOrDefault(a => a.ArrayType == 5);
            var impedanceArray = baseArray.FirstOrDefault(a => a.ArrayType == 10);
            var phaseArray = baseArray.FirstOrDefault(a => a.ArrayType == 6);

            if (frequencyArray == null || impedanceArray == null || phaseArray == null)
            {
                Debug.WriteLine("One or more required data arrays are missing.");
                throw new Exception("One or more required data arrays are missing.");
            }

            double[] frequencyValues = frequencyArray.GetValues();

            int index = Array.FindIndex(frequencyValues, freq => Math.Floor(freq) == targetFrequency);

            if (index != -1)
            {
                Debug.WriteLine($"Index of target frequency ({targetFrequency} Hz): {index}");

                // Retrieve corresponding impedance and phase values using the index
                double[] impedanceData = impedanceArray.GetValues();
                double[] phaseData = phaseArray.GetValues();

                if (index < impedanceData.Length && index < phaseData.Length)
                {
                    double impedanceValue = impedanceData[index];
                    double phaseValue = phaseData[index];
                    Debug.WriteLine($"At {targetFrequency} Hz, Impedance (|Z|): {impedanceValue}, Phase: {phaseValue} degrees");
                    return impedanceValue;
                }
            }
            else
            {
                Debug.WriteLine($"Frequency {targetFrequency} Hz not found.");
                throw new Exception($"Frequency {targetFrequency} Hz not found.");
            }

            throw new Exception("Target frequency calculation error.");
        }

        private double CalculateConcentration(double impedanceValue)
        {
            // Formula: x = (y - 0.01696) / 0.02704

            double concentration = (impedanceValue - 0.01696) / 0.02704;
            return concentration;
        }

        private async Task Continue()
        {
            //The continue will trigger the save of the measurement. //TODO maybe add cancel in case user doesn't want to save
            ActiveMeasurement.MeasurementDate = DateTime.Now.Date;
            await _measurementService.SaveMeasurement(ActiveMeasurement);
            await NavigationDispatcher.Push(NavigationViewType.HeiView);
        }

        private HeavyMetalMeasurement MockHeavyMetalMeasurement(SimpleMeasurement mockMeasurement)
        {

            var uniqueName = GenerateUniqueName("Mock Experiment");

            var someMeasurement= new HeavyMetalMeasurement
            {
                Name = uniqueName,
                Id = Guid.NewGuid(),
                Concentration = 0.0,
                Configuration = new MeasurementConfiguration(),
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
            _measurementService.CalculateConcentration();

            MeasurementIsFinished = true;
            Progress = 1;
            ProgressPercentage = 100;
        }
    }
}
