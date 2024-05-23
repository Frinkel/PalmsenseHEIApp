using PalmSens.Core.Simplified.XF.Application.Services;
using PSExampleApp.Common.Models;
using PSExampleApp.Core.Services;
using PSExampleApp.Forms.Navigation;
using PSExampleApp.Forms.Resx;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.CommunityToolkit.ObjectModel;

namespace PSExampleApp.Forms.ViewModels
{
    public class HeiViewModel : BaseAppViewModel
    {
        private readonly IPermissionService _permissionService;
        private readonly IPopupNavigation _popupNavigation;
        private readonly IUserService _userService;
        private readonly IMessageService _messageService;
        private User _activeUser;

        public HeiViewModel(
            IAppConfigurationService appConfigurationService,
            IUserService userService,
            IPermissionService permissionService,
            IMessageService messageService) : base(appConfigurationService)
        {    
            _permissionService = permissionService;
            _userService = userService;
            _popupNavigation = PopupNavigation.Instance;
            _messageService = messageService;

            ActiveUser = _userService.ActiveUser;
        }

        public User ActiveUser
        {
            get => _activeUser;
            set => SetProperty(ref _activeUser, value);
        }

        //public ICommand OnPageAppearingCommand { get; set; }

    }
}