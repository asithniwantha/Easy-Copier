using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Easy_Copier.ViewModels
{
    public sealed partial class MainViewModel
    {
        [RelayCommand]
        private void RestartAndApplyUpdate()
        {
            _updateService.RestartAndApplyUpdate();
        }
    }
}
