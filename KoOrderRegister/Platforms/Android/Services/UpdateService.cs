﻿using KoOrderRegister.Localization;
using KoOrderRegister.Services;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace KoOrderRegister.Platforms.Android.Services
{
    public class UpdateService : IAppUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "https://api.github.com/repos/BenKoncsik/KoOrderRegister/releases/latest";
        private static DateTime _lastUpdateCheck = DateTime.MinValue;
        public string AppVersion => MainActivity.AppVersion;
        public UpdateService(IHttpClientFactory httpClientFactory)
        {
            AppShell.AppVersion = AppVersion;
            _httpClient = httpClientFactory.CreateClient("GitHubClient");
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("YourApp");
        }

        public async Task<AppUpdateInfo> CheckForAppInstallerUpdatesAndLaunchAsync()
        {
            try
            {
                var (latestVersion, msixUrl) = await GetLatestReleaseInfoAsync();
                if (string.IsNullOrEmpty(latestVersion))
                {
                    return new AppUpdateInfo();
                }

                var currentVersion = MainActivity.AppVersion;
                if (new Version(latestVersion) > new Version(currentVersion))
                {
                    return new AppUpdateInfo
                    {
                        OldVersion = currentVersion,
                        NewVersion = latestVersion,
                        DownloadUrl = msixUrl
                    };

                }
#if DEBUG
                else
                {
                    return new AppUpdateInfo
                    {
                        OldVersion = currentVersion,
                        NewVersion = latestVersion,
                        DownloadUrl = msixUrl
                    };
                }
#endif
                return new AppUpdateInfo();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new AppUpdateInfo();
            }
            
        }


        public async Task<(string version, string msixUrl)> GetLatestReleaseInfoAsync()
        {
            var response = await _httpClient.GetAsync(_apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var release = JsonConvert.DeserializeObject<GitHubRelease>(jsonResponse);
                string msixUrl = string.Empty;
                string version = string.Empty;
                // Check for the correct architecture
                var architecture = "android";
                foreach (var asset in release.Assets)
                {
                    if (asset.name.ToLower().Contains(architecture.ToLower()))
                    {
                        string responseVersion = asset.browser_download_url.Split("/").Last().Split("_")[1];
                        if (string.IsNullOrEmpty(version))
                        {
                            msixUrl = asset.browser_download_url;
                            version = responseVersion;
                        }
                        else if(new Version(version) < new Version(responseVersion))
                        {
                            msixUrl = asset.browser_download_url;
                            version = responseVersion;
                        }
                    }

                }

                return (version, msixUrl);
            }

            return (null, null);
        }

        private class GitHubRelease
        {
            public string TagName { get; set; }
            public Asset[] Assets { get; set; }

            public class Asset
            {
                public string browser_download_url { get; set; }
                public string name { get; set; }
            }
        }
        public async Task<string> DownloadFileAsync(string fileUrl, IProgress<double> progress)
        {
            DownloadManager.DownloadManager.UseCustomHttpClient(_httpClient);
            return await DownloadManager.DownloadManager.DownloadAsync("KOR_update.apk", fileUrl, progress);
        }


    }
}
