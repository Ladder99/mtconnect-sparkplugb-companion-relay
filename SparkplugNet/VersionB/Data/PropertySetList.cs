﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetList.cs" company="Hämmer Electronics">
// The project is licensed under the MIT license.
// </copyright>
// <summary>
//   The externally used Sparkplug B property set list class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SparkplugNet.VersionB.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// The externally used Sparkplug B property set list class.
    /// </summary>
    public class PropertySetList
    {
        /// <summary>
        /// Gets or sets the property sets.
        /// </summary>
        public List<PropertySet> PropertySets { get; set; } = new List<PropertySet>();

        /// <summary>
        /// Gets or sets the details.
        /// </summary>
        public List<byte> Details { get; set; } = new List<byte>();
    }
}
