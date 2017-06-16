﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Models;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.IotHubManager.Services
{
    public interface IDevices
    {
        Task<IEnumerable<DeviceServiceModel>> GetListAsync();
        Task<DeviceServiceModel> GetAsync(string id);
        Task<DeviceServiceModel> CreateAsync(DeviceServiceModel toServiceModel);
    }

    public class Devices : IDevices
    {
        private const int MaxGetList = 1000;
        private readonly IServicesConfig config;
        private readonly IDeviceTwins deviceTwins;

        // Use `Lazy` to avoid parsing the connection string until necessary
        private readonly Lazy<RegistryManager> registry;
        private readonly Lazy<string> ioTHubHostName;

        public Devices(IServicesConfig config)
        {
            this.config = config;
            this.deviceTwins = new DeviceTwins(config);
            this.registry = new Lazy<RegistryManager>(this.CreateRegistry);
            this.ioTHubHostName = new Lazy<string>(this.GetIoTHubHostName);
        }

        public async Task<IEnumerable<DeviceServiceModel>> GetListAsync()
        {
            var devices = await this.registry.Value.GetDevicesAsync(MaxGetList);

            return devices.Select(azureDevice =>
                new DeviceServiceModel(azureDevice, (Twin)null, this.ioTHubHostName.Value)).ToList();
        }

        public async Task<DeviceServiceModel> GetAsync(string id)
        {
            var remoteDevice = await this.registry.Value.GetDeviceAsync(id);
            if (remoteDevice == null)
            {
                throw new ResourceNotFoundException("The device doesn't exist.");
            }

            return new DeviceServiceModel(
                remoteDevice, await this.deviceTwins.GetAsync(id), this.ioTHubHostName.Value);
        }

        public async Task<DeviceServiceModel> CreateAsync(DeviceServiceModel device)
        {
            var azureDevice = await this.registry.Value.AddDeviceAsync(device.ToAzureModel());

            // TODO: do we need to fetch the twin and return it?
            if (device.Twin == null)
            {
                return new DeviceServiceModel(azureDevice, (Twin)null, this.ioTHubHostName.Value);
            }

            // TODO: do we need to fetch the twin Etag first? - how will concurrency work?
            var azureTwin = await this.registry.Value.UpdateTwinAsync(device.Id, device.Twin.ToAzureModel(), device.Twin.Etag);
            return new DeviceServiceModel(azureDevice, azureTwin, this.ioTHubHostName.Value);
        }

        private RegistryManager CreateRegistry()
        {
            // Note: this will cause an exception if the connection string
            // is not available or badly formatted, e.g. during a test.
            return RegistryManager.CreateFromConnectionString(this.config.HubConnString);
        }

        private string GetIoTHubHostName()
        {
            return IotHubConnectionStringBuilder.Create(this.config.HubConnString).HostName;
        }
    }
}