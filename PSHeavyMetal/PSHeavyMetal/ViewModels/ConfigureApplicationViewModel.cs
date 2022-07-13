﻿using MvvmHelpers;
using PalmSens.Core.Simplified.XF.Application.Services;
using PSHeavyMetal.Core.Services;
using PSHeavyMetal.Forms.Navigation;
using PSHeavyMetal.Forms.Views;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;

namespace PSHeavyMetal.Forms.ViewModels
{
    public class ConfigureApplicationViewModel : BaseViewModel
    {
        private readonly IAppConfigurationService _appConfigurationService;
        private readonly IMessageService _messageService;
        private readonly IPopupNavigation _popupNavigation;

        public ConfigureApplicationViewModel(IMessageService messageService, IAppConfigurationService appConfigurationService)
        {
            _popupNavigation = PopupNavigation.Instance;
            _appConfigurationService = appConfigurationService;
            _messageService = messageService;
            ConfigureAnalyteCommand = CommandFactory.Create(async () => await NavigationDispatcher.Push(NavigationViewType.ConfigureAnalyteView));
            ConfigureMethodCommand = CommandFactory.Create(async () => await ConfigureMethod());
            ConfigureTitleCommand = CommandFactory.Create(async () => await ConfigureTitle());
        }

        public ICommand ConfigureAnalyteCommand { get; }

        public ICommand ConfigureMethodCommand { get; }

        public ICommand ConfigureTitleCommand { get; }

        private async Task ConfigureMethod()
        {
            var customFileType =
                new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                { DevicePlatform.iOS, new[] { "application/octet-stream" } },
                { DevicePlatform.Android, new[] { "application/octet-stream" } },
                });
            var options = new PickOptions
            {
                FileTypes = customFileType,
            };

            try
            {
                var result = await FilePicker.PickAsync(options);

                if (result != null)
                {
                    if (!result.FileName.EndsWith("psmethod"))
                    {
                        _messageService.ShortAlert("Please select a valid psmethod file");
                        return;
                    }

                    using var stream = await result.OpenReadAsync();

                    await _appConfigurationService.SaveConfigurationMethod(stream);
                    _messageService.ShortAlert("Method succesfully saved");
                }
            }
            catch (PermissionException)
            {
                _messageService.LongAlert("Failed to import file. Permissions not set");
            }
            catch (Exception)
            {
                _messageService.LongAlert("Failed importing analyte. Please check if the json file has the correct format");
            }
        }

        private async Task ConfigureTitle()
        {
            await _popupNavigation.PushAsync(new ConfigureTitlePopup());
        }
    }
}