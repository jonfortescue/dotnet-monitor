﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class MonitorApiKeyOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MonitorApiKeyOptions_Subject))]
        [Required]
        public string Subject { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MonitorApiKeyOptions_PublicKey))]
        [RegularExpression("[0-9a-zA-Z_-]+")]
        [Required]
        public string PublicKey { get; set; }
    }
}
