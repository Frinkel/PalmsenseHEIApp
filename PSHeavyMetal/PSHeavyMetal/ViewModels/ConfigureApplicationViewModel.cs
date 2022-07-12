﻿using MvvmHelpers;
using PalmSens.Core.Simplified.XF.Application.Services;
using PSHeavyMetal.Core.Services;
using PSHeavyMetal.Forms.Navigation;
using System;
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

        public ConfigureApplicationViewModel(IMessageService messageService, IAppConfigurationService appConfigurationService)
        {
            _appConfigurationService = appConfigurationService;
            _messageService = messageService;
            ConfigureAnalyteCommand = CommandFactory.Create(async () => await NavigationDispatcher.Push(NavigationViewType.ConfigureAnalyteView));
            ConfigureMethodCommand = CommandFactory.Create(async () => await ConfigureMethod());
        }

        public ICommand ConfigureAnalyteCommand { get; }

        public ICommand ConfigureMethodCommand { get; }

        private async Task ConfigureMethod()
        {
            try
            {
                var result = await FilePicker.PickAsync();

                if (result != null)
                {
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
    }
}