﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.External;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Helpers;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Models;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Runtime;
using JobStatus = Microsoft.Azure.IoTSolutions.IotHubManager.Services.Models.JobStatus;
using JobType = Microsoft.Azure.IoTSolutions.IotHubManager.Services.Models.JobType;

namespace Microsoft.Azure.IoTSolutions.IotHubManager.Services
{
    public interface IJobs
    {
        Task<IEnumerable<JobServiceModel>> GetJobsAsync(
            JobType? jobType, JobStatus? jobStatus, int? pageSize);

        Task<JobServiceModel> GetJobsAsync(string jobId);

        Task<JobServiceModel> ScheduleDeviceMethodAsync(
            string jobId,
            string queryCondition,
            MethodParameterServiceModel parameter,
            DateTimeOffset startTimeUtc,
            long maxExecutionTimeInSeconds);

        Task<JobServiceModel> ScheduleTwinUpdateAsync(
            string jobId,
            string queryCondition,
            DeviceTwinServiceModel twin,
            DateTimeOffset startTimeUtc,
            long maxExecutionTimeInSeconds);
    }

    public class Jobs : IJobs
    {
        private JobClient jobClient;
        private readonly IConfigService configService;

        public Jobs(
            IServicesConfig config,
            IConfigService configService)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            IoTHubConnectionHelper.CreateUsingHubConnectionString(
                config.HubConnString,
                conn => { this.jobClient = JobClient.CreateFromConnectionString(conn); });

            this.configService = configService;
        }

        public async Task<IEnumerable<JobServiceModel>> GetJobsAsync(
            JobType? jobType, JobStatus? jobStatus, int? pageSize)
        {
            var query = this.jobClient.CreateQuery(
                JobServiceModel.ToJobTypeAzureModel(jobType),
                JobServiceModel.ToJobStatusAzureModel(jobStatus),
                pageSize);

            var results = new List<JobServiceModel>();
            while (query.HasMoreResults)
            {
                var jobs = await query.GetNextAsJobResponseAsync();
                results.AddRange(jobs.Select(r => new JobServiceModel(r)));
            }

            return results;
        }

        public async Task<JobServiceModel> GetJobsAsync(string jobId)
        {
            var result = await this.jobClient.GetJobAsync(jobId);
            return new JobServiceModel(result);
        }

        public async Task<JobServiceModel> ScheduleTwinUpdateAsync(
            string jobId,
            string queryCondition,
            DeviceTwinServiceModel twin,
            DateTimeOffset startTimeUtc,
            long maxExecutionTimeInSeconds)
        {
            var result = await this.jobClient.ScheduleTwinUpdateAsync(
                jobId,
                queryCondition,
                twin.ToAzureModel(),
                startTimeUtc.DateTime,
                maxExecutionTimeInSeconds);

            // Update the deviceGroupFilter cache, no need to wait
            var unused = this.configService.UpdateDeviceGroupFiltersAsync(twin);

            return new JobServiceModel(result);
        }

        public async Task<JobServiceModel> ScheduleDeviceMethodAsync(
            string jobId,
            string queryCondition,
            MethodParameterServiceModel parameter,
            DateTimeOffset startTimeUtc,
            long maxExecutionTimeInSeconds)
        {
            var result = await this.jobClient.ScheduleDeviceMethodAsync(
                jobId, queryCondition,
                parameter.ToAzureModel(),
                startTimeUtc.DateTime,
                maxExecutionTimeInSeconds);
            return new JobServiceModel(result);
        }
    }
}