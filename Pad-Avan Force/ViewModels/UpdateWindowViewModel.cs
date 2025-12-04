using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using PadAwan_Force.Models;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.IO.Ports;
using System.Linq;

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

        // Software updates not implemented yet - only firmware updates
        // [ObservableProperty]
        // private bool canUpdateSoftware = false;

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

        // GitHub repository info
        private const string GITHUB_REPO_OWNER = "Ghost3379";
        private const string GITHUB_REPO_NAME = "PadAwan";
        private const string GITHUB_API_BASE = "https://api.github.com";

        public UpdateWindowViewModel(FeatherConnection? featherConnection = null)
        {
            _featherConnection = featherConnection;
            
            // Get software version from assembly
            var assembly = Assembly.GetExecutingAssembly();
            var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (versionAttribute != null)
            {
                string version = versionAttribute.InformationalVersion;
                // Remove commit hash if present (format: "1.0.0+abc123")
                int plusIndex = version.IndexOf('+');
                if (plusIndex > 0)
                {
                    version = version.Substring(0, plusIndex);
                }
                SoftwareVersion = $"v{version}";
            }
            else
            {
                var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fileVersion.FileVersion ?? "1.0.0";
                // Remove commit hash if present
                int plusIndex = version.IndexOf('+');
                if (plusIndex > 0)
                {
                    version = version.Substring(0, plusIndex);
                }
                SoftwareVersion = $"v{version}";
            }

            // Try to get firmware version from device
            _ = LoadDeviceFirmwareVersionAsync();
        }

        public async Task LoadDeviceFirmwareVersionAsync()
        {
            if (_featherConnection != null && _featherConnection.IsConnected)
            {
                DeviceFirmwareVersion = "Loading...";
                var version = await _featherConnection.GetFirmwareVersionAsync();
                if (version != null && !string.IsNullOrEmpty(version))
                {
                    DeviceFirmwareVersion = $"v{version}";
                }
                else
                {
                    DeviceFirmwareVersion = "Unknown";
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

            try
            {
                // Refresh device firmware version first
                await LoadDeviceFirmwareVersionAsync();
                
                var latestRelease = await GetLatestReleaseAsync();
                if (latestRelease != null)
                {
                    // Extract versions from release
                    LatestFirmwareVersion = latestRelease.FirmwareVersion ?? "Unknown";
                    LatestSoftwareVersion = latestRelease.SoftwareVersion ?? "Unknown";
                    
                    HasUpdateInfo = true;
                    
                    // Compare firmware versions (remove 'v' prefix for comparison)
                    string deviceVersion = DeviceFirmwareVersion.Replace("v", "").Replace("V", "").Trim();
                    string latestFirmware = LatestFirmwareVersion.Replace("v", "").Replace("V", "").Trim();
                    
                    // Allow update if device version is Unknown/Not connected OR if newer version is available
                    // This way user can update even if current version can't be read
                    bool hasNewerVersion = !string.IsNullOrEmpty(latestFirmware) && 
                                          !string.IsNullOrEmpty(deviceVersion) &&
                                          deviceVersion != "Not connected" && 
                                          deviceVersion != "Unknown" &&
                                          deviceVersion != "Loading..." &&
                                          CompareVersions(deviceVersion, latestFirmware) < 0;
                    
                    // Enable update button if version is unknown (can't compare) OR if newer version available
                    CanUpdateFirmware = deviceVersion == "Unknown" || 
                                       deviceVersion == "Not connected" ||
                                       hasNewerVersion;
                    
                    if (hasNewerVersion)
                    {
                        StatusMessage = "Firmware update available!";
                        StatusMessageColor = Brushes.LimeGreen;
                    }
                    else if (deviceVersion == "Unknown" || deviceVersion == "Not connected")
                    {
                        StatusMessage = "Firmware update available (current version unknown).";
                        StatusMessageColor = Brushes.Yellow;
                    }
                    else
                    {
                        StatusMessage = "You are running the latest firmware version!";
                        StatusMessageColor = Brushes.LimeGreen;
                    }
                }
                else
                {
                    StatusMessage = "No releases found on GitHub.";
                    StatusMessageColor = Brushes.Yellow;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error checking for updates: {ex.Message}";
                StatusMessageColor = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"Update check error: {ex}");
            }
        }

        private async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            try
            {
                string url = $"{GITHUB_API_BASE}/repos/{GITHUB_REPO_OWNER}/{GITHUB_REPO_NAME}/releases/latest";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "PadAwan-Force-Updater");
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (release != null)
                    {
                        // Extract firmware and software versions from release
                        // Format: "v1.0.0" or "Firmware: v1.0.0, Software: v1.0.0"
                        release.FirmwareVersion = ExtractVersionFromRelease(release, "firmware");
                        release.SoftwareVersion = ExtractVersionFromRelease(release, "software");
                        release.DownloadUrl = release.Assets?.FirstOrDefault(a => a.Name?.EndsWith(".bin") == true)?.BrowserDownloadUrl;
                    }
                    
                    return release;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"GitHub API error: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching release: {ex.Message}");
                return null;
            }
        }

        private string? ExtractVersionFromRelease(GitHubRelease release, string type)
        {
            // Try to extract from tag name (e.g., "v1.0.0" or "firmware-v1.0.0")
            if (release.TagName != null)
            {
                string tag = release.TagName.ToLower();
                if (type == "firmware" && tag.Contains("firmware"))
                {
                    // Extract version from tag like "firmware-v1.0.0"
                    var match = System.Text.RegularExpressions.Regex.Match(tag, @"v?(\d+\.\d+\.\d+)");
                    if (match.Success) return $"v{match.Groups[1].Value}";
                }
                else if (type == "software" && tag.Contains("software"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(tag, @"v?(\d+\.\d+\.\d+)");
                    if (match.Success) return $"v{match.Groups[1].Value}";
                }
                else if (!tag.Contains("firmware") && !tag.Contains("software"))
                {
                    // Assume tag is general version (e.g., "v1.0.0")
                    var match = System.Text.RegularExpressions.Regex.Match(tag, @"v?(\d+\.\d+\.\d+)");
                    if (match.Success) return $"v{match.Groups[1].Value}";
                }
            }
            
            // Try to extract from body/description
            if (release.Body != null)
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    release.Body, 
                    $@"{type}[:\s]+v?(\d+\.\d+\.\d+)", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                if (match.Success) return $"v{match.Groups[1].Value}";
            }
            
            return release.TagName; // Fallback to tag name
        }

        [RelayCommand]
        private async Task UpdateFirmwareAsync()
        {
            if (!CanUpdateFirmware || _featherConnection == null)
            {
                StatusMessage = "Cannot update firmware: Device connection required";
                StatusMessageColor = Brushes.Red;
                return;
            }
            
            // Save COM port before disconnecting
            string comPort = _featherConnection.ComPort;
            if (string.IsNullOrEmpty(comPort) || comPort == "None")
            {
                StatusMessage = "Cannot update firmware: No COM port available";
                StatusMessageColor = Brushes.Red;
                return;
            }

            // Set flag to prevent reconnection during update
            if (_featherConnection != null)
            {
                _featherConnection.IsUpdatingFirmware = true;
            }

            IsUpdating = true;
            UpdateStatus = "Updating firmware...";
            UpdateProgress = 0;
            StatusMessage = "";

            try
            {
                // Step 1: Get latest release info
                UpdateStatus = "Checking for firmware...";
                UpdateProgress = 10;
                var release = await GetLatestReleaseAsync();
                
                if (release == null || string.IsNullOrEmpty(release.DownloadUrl))
                {
                    StatusMessage = "No firmware file found in latest release.";
                    StatusMessageColor = Brushes.Red;
                    return;
                }

                // Step 2: Download .bin file
                UpdateStatus = "Downloading firmware...";
                UpdateProgress = 20;
                string binPath = await DownloadFirmwareAsync(release.DownloadUrl);
                
                if (string.IsNullOrEmpty(binPath) || !File.Exists(binPath))
                {
                    StatusMessage = "Failed to download firmware file.";
                    StatusMessageColor = Brushes.Red;
                    return;
                }

                // Step 3: Put device into bootloader mode and disconnect
                UpdateStatus = "Preparing device for bootloader mode...";
                UpdateProgress = 55;
                string originalPort = comPort;
                
                // First, try to put device into bootloader mode using DTR/RTS
                if (_featherConnection != null && _featherConnection.IsConnected && _featherConnection.SerialPort != null)
                {
                    try
                    {
                        var serialPort = _featherConnection.SerialPort;
                        System.Diagnostics.Debug.WriteLine("Putting device into bootloader mode...");
                        
                        // ESP32-S3 bootloader entry sequence:
                        // DTR LOW + RTS HIGH = Hold BOOT button
                        // Then toggle DTR to reset
                        serialPort.DtrEnable = false;  // DTR LOW
                        serialPort.RtsEnable = true;   // RTS HIGH (BOOT pressed)
                        await Task.Delay(100);
                        
                        // Reset: Toggle DTR
                        serialPort.DtrEnable = true;   // DTR HIGH (reset)
                        await Task.Delay(50);
                        serialPort.DtrEnable = false;  // DTR LOW
                        await Task.Delay(50);
                        
                        // Release BOOT
                        serialPort.RtsEnable = false;  // RTS LOW
                        await Task.Delay(300);
                        
                        System.Diagnostics.Debug.WriteLine("Bootloader sequence sent");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error putting device into bootloader: {ex.Message}");
                    }
                }
                
                // Now disconnect
                UpdateStatus = "Disconnecting from device...";
                UpdateProgress = 57;
                if (_featherConnection != null && _featherConnection.IsConnected)
                {
                    _featherConnection.Disconnect();
                    await Task.Delay(2000); // Give port time to release
                }
                
                // Step 3.5: Wait for device to enter bootloader mode and detect port change
                // ESP32-S3 devices change COM port when entering bootloader mode
                UpdateStatus = "Detecting bootloader port...";
                UpdateProgress = 58;
                
                string flashPort = originalPort;
                bool portChanged = false;
                
                // Wait and monitor for port change
                // The device will disappear from original port and appear on new port
                // We'll check for new ports that appear (like COM12)
                HashSet<string> seenPorts = new HashSet<string>();
                string[] initialPorts = SerialPort.GetPortNames();
                foreach (var p in initialPorts) seenPorts.Add(p);
                
                for (int i = 0; i < 30; i++) // Check for up to 15 seconds
                {
                    await Task.Delay(500);
                    
                    string[] availablePorts = SerialPort.GetPortNames();
                    
                    // Check for new ports that appeared (bootloader port)
                    foreach (var port in availablePorts)
                    {
                        if (!seenPorts.Contains(port))
                        {
                            // New port appeared - likely the bootloader port
                            flashPort = port;
                            portChanged = true;
                            System.Diagnostics.Debug.WriteLine($"New port detected: {port} (bootloader mode)");
                            seenPorts.Add(port);
                            break;
                        }
                    }
                    
                    if (portChanged)
                    {
                        // Wait a bit more for port to stabilize
                        await Task.Delay(1000);
                        break;
                    }
                    
                    // Also check if original port disappeared
                    if (!availablePorts.Contains(originalPort) && availablePorts.Length > 0)
                    {
                        // Original port gone, use first available
                        flashPort = availablePorts[0];
                        portChanged = true;
                        System.Diagnostics.Debug.WriteLine($"Original port {originalPort} disappeared, using: {flashPort}");
                        await Task.Delay(1000);
                        break;
                    }
                }
                
                // Final check - if we still have original port, but a new port appeared, use the new one
                string[] finalPorts = SerialPort.GetPortNames();
                if (finalPorts.Contains(originalPort) && finalPorts.Length > 1)
                {
                    // Multiple ports - prefer one that's not the original
                    foreach (var port in finalPorts)
                    {
                        if (port != originalPort)
                        {
                            flashPort = port;
                            portChanged = true;
                            System.Diagnostics.Debug.WriteLine($"Multiple ports available, using: {flashPort}");
                            break;
                        }
                    }
                }
                else if (!finalPorts.Contains(flashPort) && finalPorts.Length > 0)
                {
                    flashPort = finalPorts[0];
                    System.Diagnostics.Debug.WriteLine($"Port changed, using: {flashPort}");
                }
                
                System.Diagnostics.Debug.WriteLine($"Final port for flashing: {flashPort} (original: {originalPort}, changed: {portChanged})");
                comPort = flashPort; // Update comPort for flashing
                
                // Step 4: Flash firmware using esptool
                UpdateStatus = "Flashing firmware...";
                UpdateProgress = 60;
                bool success = await FlashFirmwareAsync(binPath, comPort);
                
                if (success)
                {
                    UpdateProgress = 100;
                    UpdateStatus = "Firmware updated successfully!";
                    StatusMessage = "✅ Firmware update completed! Please reconnect your device.";
                    StatusMessageColor = Brushes.LimeGreen;
                    
                    // Clean up downloaded file
                    try { File.Delete(binPath); } catch { }
                }
                else
                {
                    StatusMessage = "❌ Firmware flash failed. Check console for details.";
                    StatusMessageColor = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating firmware: {ex.Message}";
                StatusMessageColor = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"Firmware update error: {ex}");
            }
            finally
            {
                IsUpdating = false;
                if (UpdateProgress < 100) UpdateProgress = 0;
                
                // Wait a bit before clearing flag to ensure flash process is completely done
                await Task.Delay(2000);
                
                // Clear flag to allow reconnection after update
                if (_featherConnection != null)
                {
                    _featherConnection.IsUpdatingFirmware = false;
                    System.Diagnostics.Debug.WriteLine("Firmware update flag cleared - reconnection allowed");
                }
            }
        }

        private async Task<string> DownloadFirmwareAsync(string downloadUrl)
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "PadAwan-Force");
                Directory.CreateDirectory(tempDir);
                
                string fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
                if (string.IsNullOrEmpty(fileName)) fileName = "firmware.bin";
                
                string filePath = Path.Combine(tempDir, fileName);
                
                UpdateProgressText = "Downloading...";
                
                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    
                    long? totalBytes = response.Content.Headers.ContentLength;
                    long downloadedBytes = 0;
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        var buffer = new byte[8192];
                        int bytesRead;
                        
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;
                            
                            if (totalBytes.HasValue)
                            {
                                double progress = 20 + (downloadedBytes / (double)totalBytes.Value * 40); // 20-60%
                                UpdateProgress = progress;
                                UpdateProgressText = $"Downloading: {downloadedBytes / 1024} KB / {totalBytes.Value / 1024} KB";
                            }
                        }
                    }
                }
                
                UpdateProgressText = "Download complete";
                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Download error: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task<bool> FlashFirmwareAsync(string binPath, string comPort)
        {
            try
            {
                // Find esptool.py or esptool.exe
                string? esptoolPath = FindEsptool();
                
                if (string.IsNullOrEmpty(esptoolPath))
                {
                    StatusMessage = "esptool not found. Please install esptool.py or esptool.exe";
                    StatusMessageColor = Brushes.Red;
                    System.Diagnostics.Debug.WriteLine("ERROR: esptool not found");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"Using esptool: {esptoolPath}");
                System.Diagnostics.Debug.WriteLine($"COM Port: {comPort}");
                System.Diagnostics.Debug.WriteLine($"Firmware file: {binPath}");

                // Helper function to get current available port
                // If preferred port fails, try to find alternative port
                string GetAvailablePort(string preferredPort)
                {
                    string[] availablePorts = SerialPort.GetPortNames();
                    
                    // If preferred port is in list, try it first
                    if (availablePorts.Contains(preferredPort))
                        return preferredPort;
                    
                    // Otherwise, use first available port
                    if (availablePorts.Length > 0)
                        return availablePorts[0];
                    
                    return preferredPort; // Fallback
                }
                
                // Helper function to find alternative port if current one fails
                string FindAlternativePort(string failedPort)
                {
                    string[] availablePorts = SerialPort.GetPortNames();
                    foreach (var port in availablePorts)
                    {
                        if (port != failedPort)
                        {
                            System.Diagnostics.Debug.WriteLine($"Found alternative port: {port}");
                            return port;
                        }
                    }
                    return failedPort; // No alternative found
                }

                UpdateProgressText = "Erasing flash...";
                UpdateProgress = 60;
                
                // Step 1: Erase flash (use new command format: erase-flash instead of erase_flash)
                // Check port before each operation
                string currentPort = GetAvailablePort(comPort);
                if (currentPort != comPort)
                {
                    System.Diagnostics.Debug.WriteLine($"Port changed during erase: {comPort} -> {currentPort}");
                    comPort = currentPort;
                }
                
                bool eraseSuccess = await RunEsptoolAsync(esptoolPath, comPort, new[] { "erase-flash" });
                if (!eraseSuccess)
                {
                    // Port might have changed - find alternative port
                    string retryPort = FindAlternativePort(comPort);
                    if (retryPort != comPort)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erase failed on {comPort}, retrying with alternative port: {retryPort}");
                        comPort = retryPort;
                        await Task.Delay(1000); // Wait for port to be ready
                        eraseSuccess = await RunEsptoolAsync(esptoolPath, comPort, new[] { "erase-flash" });
                    }
                    
                    if (!eraseSuccess)
                    {
                        StatusMessage = "Failed to erase flash. Check console for details.";
                        StatusMessageColor = Brushes.Red;
                        System.Diagnostics.Debug.WriteLine("ERROR: Flash erase failed");
                        return false;
                    }
                }

                UpdateProgressText = "Writing firmware...";
                UpdateProgress = 70;
                
                // Step 2: Write firmware (for merged.bin, write to 0x0)
                // Check port before write
                currentPort = GetAvailablePort(comPort);
                if (currentPort != comPort)
                {
                    System.Diagnostics.Debug.WriteLine($"Port changed during write: {comPort} -> {currentPort}");
                    comPort = currentPort;
                }
                
                bool writeSuccess = await RunEsptoolAsync(esptoolPath, comPort, new[] 
                { 
                    "write_flash", 
                    "0x0", 
                    binPath 
                });
                
                if (!writeSuccess)
                {
                    // Port might have changed - find alternative port
                    string retryPort = FindAlternativePort(comPort);
                    if (retryPort != comPort)
                    {
                        System.Diagnostics.Debug.WriteLine($"Write failed on {comPort}, retrying with alternative port: {retryPort}");
                        comPort = retryPort;
                        await Task.Delay(1000); // Wait for port to be ready
                        writeSuccess = await RunEsptoolAsync(esptoolPath, comPort, new[] 
                        { 
                            "write_flash", 
                            "0x0", 
                            binPath 
                        });
                    }
                    
                    if (!writeSuccess)
                    {
                        StatusMessage = "Failed to write firmware. Check console for details.";
                        StatusMessageColor = Brushes.Red;
                        System.Diagnostics.Debug.WriteLine("ERROR: Flash write failed");
                        return false;
                    }
                }

                UpdateProgressText = "Verifying...";
                UpdateProgress = 90;
                
                // Step 3: Verify
                // Check port before verify
                currentPort = GetAvailablePort(comPort);
                if (currentPort != comPort)
                {
                    System.Diagnostics.Debug.WriteLine($"Port changed during verify: {comPort} -> {currentPort}");
                    comPort = currentPort;
                }
                
                bool verifySuccess = await RunEsptoolAsync(esptoolPath, comPort, new[] 
                { 
                    "verify_flash", 
                    "0x0", 
                    binPath 
                });
                
                UpdateProgress = 95;
                
                if (!verifySuccess)
                {
                    // Port might have changed - find alternative port
                    string retryPort = FindAlternativePort(comPort);
                    if (retryPort != comPort)
                    {
                        System.Diagnostics.Debug.WriteLine($"Verify failed on {comPort}, retrying with alternative port: {retryPort}");
                        comPort = retryPort;
                        await Task.Delay(1000); // Wait for port to be ready
                        verifySuccess = await RunEsptoolAsync(esptoolPath, comPort, new[] 
                        { 
                            "verify_flash", 
                            "0x0", 
                            binPath 
                        });
                    }
                    
                    if (!verifySuccess)
                    {
                        StatusMessage = "Verification failed. Check console for details.";
                        StatusMessageColor = Brushes.Red;
                        System.Diagnostics.Debug.WriteLine("ERROR: Flash verify failed");
                    }
                }
                
                return verifySuccess;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Flash error: {ex.Message}";
                StatusMessageColor = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"Flash error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private string? FindEsptool()
        {
            System.Diagnostics.Debug.WriteLine("Searching for esptool...");
            
            // First, try bundled esptool.exe in Tools folder (next to executable)
            string? exeLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDir = exeLocation != null ? Path.GetDirectoryName(exeLocation) ?? "" : "";
            string bundledEsptool = Path.Combine(exeDir, "esptool.exe");
            if (File.Exists(bundledEsptool))
            {
                System.Diagnostics.Debug.WriteLine($"Found bundled esptool.exe: {bundledEsptool}");
                return bundledEsptool;
            }
            
            // Also try in Tools subdirectory
            string toolsEsptool = Path.Combine(exeDir, "Tools", "esptool.exe");
            if (File.Exists(toolsEsptool))
            {
                System.Diagnostics.Debug.WriteLine($"Found esptool.exe in Tools folder: {toolsEsptool}");
                return toolsEsptool;
            }
            
            // Try esptool.exe in PATH
            try
            {
                var whichProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "where",
                        Arguments = "esptool.exe",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                whichProcess.Start();
                string output = whichProcess.StandardOutput.ReadToEnd();
                whichProcess.WaitForExit(2000);
                if (whichProcess.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    string path = output.Split('\n')[0].Trim();
                    if (File.Exists(path))
                    {
                        System.Diagnostics.Debug.WriteLine($"Found esptool.exe in PATH: {path}");
                        return path;
                    }
                }
            }
            catch { }
            
            // Try esptool.py via python - check multiple Python installations
            string[] pythonCommands = new[] { "python", "python3", "py" };
            
            // Also try the specific Python path from the terminal output
            string[] pythonPaths = new[]
            {
                @"C:\Users\nicol\AppData\Local\Programs\Python\Python312\python.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python312", "python.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python*", "python.exe")
            };
            
            // First try specific paths
            foreach (var pythonPath in pythonPaths)
            {
                if (pythonPath.Contains("*"))
                {
                    // Search for Python installations
                    var pythonDir = Path.GetDirectoryName(pythonPath);
                    if (Directory.Exists(pythonDir))
                    {
                        var pythonDirs = Directory.GetDirectories(pythonDir, "Python*");
                        foreach (var dir in pythonDirs)
                        {
                            var fullPath = Path.Combine(dir, "python.exe");
                            if (File.Exists(fullPath))
                            {
                                try
                                {
                                    var checkProcess = new Process
                                    {
                                        StartInfo = new ProcessStartInfo
                                        {
                                            FileName = fullPath,
                                            Arguments = "-m esptool version",
                                            UseShellExecute = false,
                                            RedirectStandardOutput = true,
                                            RedirectStandardError = true,
                                            CreateNoWindow = true
                                        }
                                    };
                                    checkProcess.Start();
                                    checkProcess.WaitForExit(3000);
                                    if (checkProcess.ExitCode == 0)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Found esptool via {fullPath}");
                                        return fullPath;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                else if (File.Exists(pythonPath))
                {
                    try
                    {
                        var checkProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = pythonPath,
                                Arguments = "-m esptool version",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };
                        checkProcess.Start();
                        checkProcess.WaitForExit(3000);
                        if (checkProcess.ExitCode == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Found esptool via {pythonPath}");
                            return pythonPath;
                        }
                    }
                    catch { }
                }
            }
            
            // Then try commands in PATH
            foreach (var pythonCmd in pythonCommands)
            {
                try
                {
                    var checkProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = pythonCmd,
                            Arguments = "-m esptool version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    checkProcess.Start();
                    checkProcess.WaitForExit(3000);
                    if (checkProcess.ExitCode == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found esptool via {pythonCmd}");
                        return pythonCmd;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error checking {pythonCmd}: {ex.Message}");
                }
            }
            
            // Try common installation paths
            string[] possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Arduino15", "packages", "esp32", "tools", "esptool_py", "*", "esptool.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".platformio", "penv", "Scripts", "esptool.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".platformio", "penv", "Scripts", "esptool"),
            };

            foreach (var path in possiblePaths)
            {
                if (path.Contains("*"))
                {
                    // Search in directory
                    var dir = Path.GetDirectoryName(path);
                    var pattern = Path.GetFileName(path);
                    if (Directory.Exists(dir))
                    {
                        var files = Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);
                        if (files.Length > 0) return files[0];
                    }
                }
                else
                {
                    // Check if command exists
                    try
                    {
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = path,
                                Arguments = "--version",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };
                        process.Start();
                        process.WaitForExit(2000);
                        if (process.ExitCode == 0 || path == "python" || path == "python3")
                        {
                            // For python, check if esptool module is available
                            if (path == "python" || path == "python3")
                            {
                                var checkProcess = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = path,
                                        Arguments = "-m esptool version",
                                        UseShellExecute = false,
                                        RedirectStandardOutput = true,
                                        RedirectStandardError = true,
                                        CreateNoWindow = true
                                    }
                                };
                                checkProcess.Start();
                                checkProcess.WaitForExit(2000);
                                if (checkProcess.ExitCode == 0)
                                {
                                    return path; // Return python, we'll use "python -m esptool"
                                }
                            }
                            else
                            {
                                return path;
                            }
                        }
                    }
                    catch { }
                }
            }

            return null;
        }

        private async Task<bool> RunEsptoolAsync(string esptoolPath, string comPort, string[] arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Build arguments
                // Note: esptool automatically handles bootloader entry via DTR/RTS
                // We don't need to manually put device in bootloader mode
                var argsList = new List<string> { "--port", comPort, "--baud", "921600", "--chip", "esp32s3" };
                argsList.AddRange(arguments);
                
                // If esptoolPath is python/python3/py or ends with python.exe, use "python -m esptool"
                bool isPython = esptoolPath == "python" || 
                               esptoolPath == "python3" || 
                               esptoolPath == "py" ||
                               esptoolPath.EndsWith("python.exe", StringComparison.OrdinalIgnoreCase) ||
                               esptoolPath.EndsWith("python3.exe", StringComparison.OrdinalIgnoreCase);
                
                if (isPython)
                {
                    processInfo.FileName = esptoolPath;
                    // Insert "-m esptool" at the beginning
                    argsList.Insert(0, "esptool");
                    argsList.Insert(0, "-m");
                }
                else
                {
                    processInfo.FileName = esptoolPath;
                }
                
                processInfo.Arguments = string.Join(" ", argsList);

                System.Diagnostics.Debug.WriteLine($"Running: {processInfo.FileName} {processInfo.Arguments}");
                Console.WriteLine($"Running: {processInfo.FileName} {processInfo.Arguments}");

                var process = new Process { StartInfo = processInfo };
                
                var output = new System.Text.StringBuilder();
                var error = new System.Text.StringBuilder();
                
                process.OutputDataReceived += (s, e) => 
                { 
                    if (e.Data != null) 
                    {
                        output.AppendLine(e.Data);
                        System.Diagnostics.Debug.WriteLine($"esptool output: {e.Data}");
                        Console.WriteLine($"esptool output: {e.Data}");
                    }
                };
                process.ErrorDataReceived += (s, e) => 
                { 
                    if (e.Data != null) 
                    {
                        error.AppendLine(e.Data);
                        System.Diagnostics.Debug.WriteLine($"esptool error: {e.Data}");
                        Console.WriteLine($"esptool error: {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                bool exited = await Task.Run(() => process.WaitForExit(120000)); // 120 second timeout for large files
                
                if (!exited)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: esptool timed out");
                    Console.WriteLine("ERROR: esptool timed out");
                    try { process.Kill(); } catch { }
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine($"esptool exit code: {process.ExitCode}");
                Console.WriteLine($"esptool exit code: {process.ExitCode}");
                
                if (process.ExitCode != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"esptool failed. Output: {output}");
                    System.Diagnostics.Debug.WriteLine($"esptool error output: {error}");
                    Console.WriteLine($"esptool failed. Output: {output}");
                    Console.WriteLine($"esptool error output: {error}");
                }

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"esptool execution error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine($"esptool execution error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        // Software updates not implemented - only firmware updates available
        // [RelayCommand]
        // private async Task UpdateSoftwareAsync() { }

        // Helper method to compare version strings (e.g., "v1.0.0" vs "v1.0.1")
        private int CompareVersions(string version1, string version2)
        {
            // Remove 'v' prefix if present
            version1 = version1.TrimStart('v', 'V').Trim();
            version2 = version2.TrimStart('v', 'V').Trim();

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

    // GitHub API Models
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("body")]
        public string? Body { get; set; }
        
        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
        
        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }
        
        // Extracted versions (not from API)
        public string? FirmwareVersion { get; set; }
        public string? SoftwareVersion { get; set; }
        public string? DownloadUrl { get; set; }
    }

    public class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
        
        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}

