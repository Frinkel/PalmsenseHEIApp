using PalmSens.Core.Simplified.XF.Application.Services;
using PSExampleApp.Common.Models;
using PSExampleApp.Core.Services;
using PSExampleApp.Forms.Navigation;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;

namespace PSExampleApp.Forms.ViewModels
{
    public class HeiViewModel : BaseAppViewModel
    {
        private readonly IMeasurementService _measurementService;
        private readonly IUserService _userService;
        private readonly IMessageService _messageService;
        private readonly IPopupNavigation _popupNavigation;
        private readonly IShareService _shareService;
        private bool _isCreatingReport;
        private HeavyMetalMeasurement _activeMeasurement;

        //private bool _hasMax
        private HeavyMetalMeasurement _loadedMeasurement;

        public HeiViewModel(IMeasurementService measurementService, IShareService shareService, IAppConfigurationService appConfigurationService, IMessageService messageService, IUserService userService) : base(appConfigurationService)
        {
            _measurementService = measurementService;
            _messageService = messageService;
            _shareService = shareService;
            _userService = userService;
            ActiveMeasurement = _measurementService.ActiveMeasurement;
            _popupNavigation = PopupNavigation.Instance;
            NavigateToHomeCommand = CommandFactory.Create(NavigateToHome);
            Task.Run(() => this.InitializeActiveMeasurement()).Wait();
            
        }

        public HeavyMetalMeasurement ActiveMeasurement
        {
            get => _activeMeasurement;
            set
            {
                if (_activeMeasurement != value)
                {
                    _activeMeasurement = value;
                    OnPropertyChanged(nameof(ActiveMeasurement));
                    OnPropertyChanged(nameof(ViewFriendlyConcentration));
                }
            }
        }

        public string ViewFriendlyLinearEquation
        {
            get
            {
                return (!(_activeMeasurement.LinearEquationConfiguration.Intercept == 0.0 || _activeMeasurement.LinearEquationConfiguration.Slope == 0.0))
                    ? $"Linear Equation: y = {_activeMeasurement.LinearEquationConfiguration.Intercept} + {_activeMeasurement.LinearEquationConfiguration.Slope} * x"
                    : "No Linear Eq Configured.";
            }
        }

        public string ViewFriendlyTargetFrequency
        {
            get
            {
                return $"Target frequency: {_activeMeasurement.TargetFrequency}";
            }
        }

        public string ViewFriendlyBatchNumber
        {
            get
            {
                return !((_activeMeasurement.LinearEquationConfiguration.Intercept == 0.0 || _activeMeasurement.LinearEquationConfiguration.Slope == 0.0)) 
                ? $"Batch Number: {_activeMeasurement.LinearEquationConfiguration.BatchNumber}"
                : "No linear Eq Configured";
            }
        }

        private async void InitializeActiveMeasurement()
        {
            ActiveMeasurement = await _measurementService.GetActiveMeasurement().ConfigureAwait(false);
        }

        public string ViewFriendlyConcentration
        {
            get => ActiveMeasurement != null
                ? $"Concentration: {Math.Round(ActiveMeasurement.HeiConcentration, 2)} mIU/L"
                : "Concentration: N/A";
        }

        public ICommand NavigateToHomeCommand { get; }

        public async Task NavigateToHome()
        {
            _measurementService.ResetMeasurement();
            await NavigationDispatcher.PopToRoot();
        }

    }
}