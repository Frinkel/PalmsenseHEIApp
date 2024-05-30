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
using PSExampleApp.Forms.Resx;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;

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
            ContinueCommand = CommandFactory.Create(Continue);
            SelectDataCommand = CommandFactory.Create(OnSelectData);

        }

        //public ICommand LoadMockDataCommand { get; }
        public ICommand OnPageAppearingCommand { get; }
        public ICommand ContinueCommand { get; }
        public ICommand SelectDataCommand { get; }

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

        private async Task OnSelectData()
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
                    if (!result.FileName.EndsWith("pssession"))
                    {
                        _messageService.ShortAlert(AppResources.Alert_SelectValidSessionFile);
                        return;
                    }

                    var measurement = await _loadSavePlatformService.LoadMeasurementFromFileAsync(result.FullPath);
                    _messageService.ShortAlert(AppResources.Alert_MethodSaved);

                    LoadMockData(measurement);
                    await Continue();
                }
            }
            catch (PermissionException)
            {
                _messageService.LongAlert(AppResources.Alert_FailedImport);
            }
            catch (Exception)
            {
                _messageService.LongAlert(AppResources.Alert_FailedImportSession);
            }
        }

        private void LoadMockData(SimpleMeasurement mockMeasurement)
        {
            try
            {
                ActiveMeasurement = MockHeavyMetalMeasurement(mockMeasurement);
                _measurementService.SetActiveMeasurement(ActiveMeasurement);

                double targetFrequency = 126.0;
                double concentration = _measurementService.HeiCalculateConcentration(targetFrequency);

                ActiveMeasurement.HeiConcentration = concentration;

                MeasurementIsFinished = true;
                Progress = 1;
                ProgressPercentage = 100;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading mock data: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
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
                MeasurementDate = DateTime.Now.Date,
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
    }
}
