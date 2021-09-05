﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SparkplugApplication.cs" company="Hämmer Electronics">
// The project is licensed under the MIT license.
// </copyright>
// <summary>
//   Defines the SparkplugApplication type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SparkplugNet.VersionB
{
    using System.Collections.Generic;

    using Serilog;

    using SparkplugNet.Core.Application;
    using SparkplugNet.VersionB.Data;

    /// <inheritdoc cref="SparkplugApplicationBase{T}"/>
    public class SparkplugApplication : SparkplugApplicationBase<Metric>
    {
        /// <inheritdoc cref="SparkplugApplicationBase{T}"/>
        /// <summary>
        /// Initializes a new instance of the <see cref="SparkplugApplication"/> class.
        /// </summary>
        /// <param name="knownMetrics">The known metrics.</param>
        /// <param name="logger">The logger.</param>
        public SparkplugApplication(List<Metric> knownMetrics, ILogger? logger = null) : base(knownMetrics, logger)
        {
        }
    }
}