﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi.Validation;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    partial class DiagController
    {
        /// <summary>
        /// Capture metrics for a process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="durationSeconds">The duration of the metrics session (in seconds).</param>
        /// <param name="egressProvider">The egress provider to which the metrics are saved.</param>
        [HttpGet("livemetrics", Name = nameof(CaptureMetrics))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJsonSequence)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [RequestLimit(LimitKey = Utilities.ArtifactType_Metrics)]
        [EgressValidation]
        public Task<ActionResult> CaptureMetrics(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            string egressProvider = null)
        {
            ProcessKey? processKey = GetProcessKey(pid, uid, name);

            return InvokeForProcess(async (processInfo) =>
            {
                string fileName = GetMetricFilename(processInfo);

                Func<Stream, CancellationToken, Task> action = async (outputStream, token) =>
                {
                    var client = new DiagnosticsClient(processInfo.EndpointInfo.Endpoint);
                    EventPipeCounterPipelineSettings settings = EventCounterSettingsFactory.CreateSettings(
                        _counterOptions.CurrentValue,
                        includeDefaults: true,
                        durationSeconds: durationSeconds);

                    await using EventCounterPipeline eventCounterPipeline = new EventCounterPipeline(client,
                        settings,
                        loggers:
                        new[] { new JsonCounterLogger(outputStream) });

                    await eventCounterPipeline.RunAsync(token);
                };

                return await Result(Utilities.ArtifactType_Metrics,
                    egressProvider,
                    action,
                    fileName,
                    ContentTypes.ApplicationJsonSequence,
                    processInfo.EndpointInfo);
            }, processKey, Utilities.ArtifactType_Metrics);
        }

        /// <summary>
        /// Capture metrics for a process.
        /// </summary>
        /// <param name="configuration">The metrics configuration describing which metrics to capture.</param>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="durationSeconds">The duration of the metrics session (in seconds).</param>
        /// <param name="egressProvider">The egress provider to which the metrics are saved.</param>
        [HttpPost("livemetrics", Name = nameof(CaptureMetricsCustom))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJsonSequence)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [RequestLimit(LimitKey = Utilities.ArtifactType_Metrics)]
        [EgressValidation]
        public Task<ActionResult> CaptureMetricsCustom(
            [FromBody][Required]
            Models.EventMetricsConfiguration configuration,
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            string egressProvider = null)
        {
            ProcessKey? processKey = GetProcessKey(pid, uid, name);

            return InvokeForProcess(async (processInfo) =>
            {
                string fileName = GetMetricFilename(processInfo);

                Func<Stream, CancellationToken, Task> action = async (outputStream, token) =>
                {
                    var client = new DiagnosticsClient(processInfo.EndpointInfo.Endpoint);
                    EventPipeCounterPipelineSettings settings = EventCounterSettingsFactory.CreateSettings(
                        _counterOptions.CurrentValue,
                        durationSeconds,
                        configuration);

                    await using EventCounterPipeline eventCounterPipeline = new EventCounterPipeline(client,
                        settings,
                        loggers:
                        new[] { new JsonCounterLogger(outputStream) });

                    await eventCounterPipeline.RunAsync(token);
                };

                return await Result(Utilities.ArtifactType_Metrics,
                    egressProvider,
                    action,
                    fileName,
                    ContentTypes.ApplicationJsonSequence,
                    processInfo.EndpointInfo);
            }, processKey, Utilities.ArtifactType_Metrics);
        }

        private static string GetMetricFilename(IProcessInfo processInfo) =>
            FormattableString.Invariant($"{Utilities.GetFileNameTimeStampUtcNow()}_{processInfo.EndpointInfo.ProcessId}.metrics.json");
    }
}
