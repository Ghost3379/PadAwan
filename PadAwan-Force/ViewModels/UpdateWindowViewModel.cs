using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using PadAwan_Force.Models;
using System.Net.Http;
using System.Text.Json;
using System.IO;
using System.IO.Ports;

namespace PadAwan_Force.ViewModels
{
    public partial class UpdateWindowViewModel : ViewModelBase
    {
        private readonly FeatherConnection? _featherConnection;
        private readonly HttpClient _httpClient = new HttpClient();

        [ObservableProperty]
        private string deviceFirmwareVersion = "Unknown";

        [ObservableProperty]
        private string softwareVersion = "1.0.0";

        [ObservableProperty]
        private string latestFirmwareVersion = "";

        [ObservableProperty]
        private string latestSoftwareVersion = "";

        [ObservableProperty]
        private bool hasUpdateInfo = false;

        [ObservableProperty]
        private bool canUpdateFirmware = false;

        [ObservableProperty]
        private bool canUpdateSoftware = false;

        [ObservableProperty]
        private bool isUpdating = false;

        [ObservableProperty]
        private double updateProgress = 0;

        [ObservableProperty]
        private string updateStatus = "";

        [ObservableProperty]
        private string updateProgressText = "";

        [ObservableProperty]
        private string statusMessage = "";

        [ObservableProperty]
        private IBrush statusMessageColor = Brushes.White;

        // GitHub repository info (TODO: Replace with actual repo)
        private const string GITHUB_REPO_OWNER = "yourusername";
        private const string GITHUB_REPO_NAME = "PadAwan-Force";

        public UpdateWindowViewModel(FeatherConnection? featherConnection = null)
        {
            _featherConnection = featherConnection;
            
            // Get software version from assembly
            var assembly = Assembly.GetExecutingAssembly();
            var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (versionAttribute != null)
            {
                SoftwareVersion = $"v{versionAttribute.InformationalVersion}";
            }
            else
            {
                var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
                SoftwareVersion = $"v{fileVersion.FileVersion ?? "1.0.0"}";
            }

            // Try to get firmware version from device
            _ = LoadDeviceFirmwareVersionAsync();
        }

        private async Task LoadDeviceFirmwareVersionAsync()
        {
            if (_featherConnection != null && _featherConnection.IsConnected)
            {
                var version = await _featherConnection.GetFirmwareVersionAsync();
                if (version != null)
                {
                    DeviceFirmwareVersion = $"v{version}";
                }
                else
                {
                    DeviceFirmwareVersion = "Not connected";
                }
            }
            else
            {
                DeviceFirmwareVersion = "Not connected";
            }
        }

        [RelayCommand]
        private async Task CheckForUpdatesAsync()
        {
            StatusMessage = "Checking for updates...";
            StatusMessageColor = Brushes.White;
            HasUpdateInfo = false;
            CanUpdateFirmware = false;
            CanUpdateSoftware = false;

            try
            {
                // TODO: Implement GitHub API check
                // For now, just show a placeholder message
                StatusMessage = "Update check not yet implemented. GitHub API integration needed.";
                StatusMessageColor = Brushes.Yellow;
                
                // Example structure (commented out until GitHub API is set up):
                /*
                var latestRelease = await GetLatestReleaseAsync();
                if (latestRelease != null)
                {
                    LatestFirmwareVersion = latestRelease.FirmwareVersion;
                    LatestSoftwareVersion = latestRelease.SoftwareVersion;
                    
                    HasUpdateInfo = true;
                    CanUpdateFirmware = CompareVersions(DeviceFirmwareVersion, LatestFirmwareVersion) < 0;
                    CanUpdateSoftware = CompareVersions(SoftwareVersion, LatestSoftwareVersion) < 0;
                    
                    if (!CanUpdateFirmware && !CanUpdateSoftware)
                    {
                        StatusMessage = "You are running the latest versions!";
                        StatusMessageColor = Brushes.LimeGreen;
                    }
                }
                */
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error checking for updates: {ex.Message}";
                StatusMessageColor = Brushes.Red;
            }
        }

        [RelayCommand]
        private async Task UpdateFirmwareAsync()
        {
            if (!CanUpdateFirmware || _featherConnection == null || !_featherConnection.IsConnected)
            {
                StatusMessage = "Cannot update firmware: Device not connected";
                StatusMessageColor = Brushes.Red;
                return;
            }

            IsUpdating = true;
            UpdateStatus = "Updating firmware...";
            UpdateProgress = 0;
            StatusMessage = "";

            try
            {
                // TODO: Download .bin file from GitHub
                // TODO: Use esptool to flash the firmware
                
                StatusMessage = "Firmware update not yet implemented. esptool integration needed.";
                StatusMessageColor = Brushes.Yellow;
                
                // Example structure (commented out until esptool is integrated):
                /*
                string binPath = await DownloadFirmwareAsync();
                await FlashFirmwareAsync(binPath);
                */
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating firmware: {ex.Message}";
                StatusMessageColor = Brushes.Red;
            }
            finally
            {
                IsUpdating = false;
                UpdateProgress = 0;
            }
        }

        [RelayCommand]
        private async Task UpdateSoftwareAsync()
        {
            StatusMessage = "Software update not yet implemented.";
            StatusMessageColor = Brushes.Yellow;
            
            // Software updates would require:
            // 1. Download new installer/executable
            // 2. Close current application
            // 3. Run installer
            // This is more complex and might be better handled by an installer system
        }

        // Helper method to compare version strings (e.g., "v1.0.0" vs "v1.0.1")
        private int CompareVersions(string version1, string version2)
        {
            // Remove 'v' prefix if present
            version1 = version1.TrimStart('v', 'V');
            version2 = version2.TrimStart('v', 'V');

            var v1Parts = version1.Split('.');
            var v2Parts = version2.Split('.');

            int maxLength = Math.Max(v1Parts.Length, v2Parts.Length);
            for (int i = 0; i < maxLength; i++)
            {
                int v1Part = i < v1Parts.Length && int.TryParse(v1Parts[i], out int v1) ? v1 : 0;
                int v2Part = i < v2Parts.Length && int.TryParse(v2Parts[i], out int v2) ? v2 : 0;

                if (v1Part < v2Part) return -1;
                if (v1Part > v2Part) return 1;
            }

            return 0;
        }
    }
}

