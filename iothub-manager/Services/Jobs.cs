﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Models;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Runtime;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Azure.IoTSolutions.IotHubManager.Services
{
    public interface IJobs
    {
        Task<IEnumerable<JobServiceModel>> GetJobsAsync(JobType? jobType, JobStatus? jobStatus, int? pageSize);
        Task<JobServiceModel> GetJobsAsync(string jobId);
        Task<JobServiceModel> ScheduleDeviceMethodAsync(string jobId, string queryCondition, MethodParameterServiceModel parameter, DateTime startTimeUtc, long maxExecutionTimeInSeconds);
        Task<JobServiceModel> ScheduleTwinUpdateAsync(string jobId, string queryCondition, DeviceTwinServiceModel twin, DateTime startTimeUtc, long maxExecutionTimeInSeconds);
    }

    public class Jobs : IJobs
    {
        private readonly Azure.Devices.JobClient jobClient;

        public Jobs(IServicesConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            this.jobClient = Azure.Devices.JobClient.CreateFromConnectionString(config.HubConnString);
        }

        public async Task<IEnumerable<JobServiceModel>> GetJobsAsync(JobType? jobType, JobStatus? jobStatus, int? pageSize)
        {
            var query = this.jobClient.CreateQuery(JobServiceModel.ToJobTypeAzureModel(jobType), JobServiceModel.ToJobStatusAzureModel(jobStatus), pageSize);
            var result = await query.GetNextAsJobResponseAsync();
            return result.Select(r => new JobServiceModel(r));
        }

        public async Task<JobServiceModel> GetJobsAsync(string jobId)
        {
            var result = await this.jobClient.GetJobAsync(jobId);
            return new JobServiceModel(result);
        }

        public async Task<JobServiceModel> ScheduleTwinUpdateAsync(string jobId, string queryCondition, DeviceTwinServiceModel twin, DateTime startTimeUtc, long maxExecutionTimeInSeconds)
        {
            var result = await this.jobClient.ScheduleTwinUpdateAsync(jobId, queryCondition, twin.ToAzureModel(), startTimeUtc, maxExecutionTimeInSeconds);
            return new JobServiceModel(result);
        }

        public async Task<JobServiceModel> ScheduleDeviceMethodAsync(string jobId, string queryCondition, MethodParameterServiceModel parameter, DateTime startTimeUtc, long maxExecutionTimeInSeconds)
        {
            var result = await this.jobClient.ScheduleDeviceMethodAsync(jobId, queryCondition, parameter.ToAzureModel(), startTimeUtc, maxExecutionTimeInSeconds);
            return new JobServiceModel(result);
        }
    }
}