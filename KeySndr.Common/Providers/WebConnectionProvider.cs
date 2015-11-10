﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KeySndr.Common.Providers
{
    public class WebConnectionProvider : IWebConnectionProvider
    {
        private const string AllConfigurations = "api/action/getallconfigurations";
        private const string AllScripts = "api/scripts/getallscripts";
        private const string Configuration = "api/action/getconfiguration";
        private const string Save = "api/action/save";
        private const string Execute = "api/action/execute";

        private readonly HttpClient httpClient;

        public WebConnectionProvider()
        {
            httpClient = new HttpClient()
            {
                Timeout = new TimeSpan(0, 0, 2)
            };
        }

        public void SetBaseAddress(string ip, int port)
        {
            httpClient.BaseAddress = new Uri($"http://{ip}:{port}/");
        }

        public async Task<ApiResult<IEnumerable<string>>> RequestConfigurations()
        {
            var result = await httpClient.GetAsync(AllConfigurations);
            var content = await result.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResult<IEnumerable<string>>>(content);
        }

        public async Task<ApiResult<IEnumerable<string>>> RequestScripts()
        {
            var result = await httpClient.GetAsync(AllScripts);
            var content = await result.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResult<IEnumerable<string>>>(content);
        }

        public async Task<ApiResult<InputConfiguration>> RequestConfiguration(string name)
        {
            var result = await httpClient.GetAsync(Configuration + "?name=" + name);
            var content = await result.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResult<InputConfiguration>>(content);
        }

        public async Task<ApiResult<Object>>  SaveConfiguration(InputConfiguration configuration)
        {
            var result = await httpClient.PostAsync(Save,
                        new StringContent(JsonSerializer.Serialize(configuration), Encoding.UTF8, "application/json"));
            var content = await result.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResult<Object>>(content);
        }

        public async Task<ApiResult<Object>> ExecuteAction(InputAction action)
        {
            var result = await httpClient.PostAsync(Execute,
                        new StringContent(JsonSerializer.Serialize(action), Encoding.UTF8, "application/json"));
            var content = await result.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResult<Object>>(content);
        }
    }
}
