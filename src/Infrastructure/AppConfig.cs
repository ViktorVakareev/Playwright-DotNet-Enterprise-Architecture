using System;

namespace WorldBank.Automation.Tests.Infrastructure
{
    public static class AppConfig
    {
        public static string GetBaseUrl()
        {
            // 1. Read the environment variable injected by Jenkins
            // 2. If it's null (e.g., running locally in Visual Studio), default to "test"
            string targetEnv = Environment.GetEnvironmentVariable("TARGET_ENV") ?? "test";
            var baseUrl = new Uri("https://viktorvakareev.github.io/Playwright-DotNet-Enterprise-Architecture/WorldBankMockApp/");
            var finalUrl = new Uri(baseUrl, $"{targetEnv}").ToString();

            // 3. Construct the dynamic GitHub Pages URL
            return finalUrl;
        }
    }
}