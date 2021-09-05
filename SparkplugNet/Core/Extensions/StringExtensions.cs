﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Hämmer Electronics">
// The project is licensed under the MIT license.
// </copyright>
// <summary>
//   A class that contains extension method for all <see cref="string"/> data type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SparkplugNet.Core.Extensions
{
    /// <summary>
    /// A class that contains extension method for all <see cref="string"/> data type.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Checks whether the given identifier is valid or not.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if the given identifier is valid, <c>false</c> else.</returns>
        internal static bool IsIdentifierValid(this string value)
        {
            return !string.IsNullOrWhiteSpace(value) && !value.Contains("/") && !value.Contains("#") && !value.Contains("+");
        }
    }
}