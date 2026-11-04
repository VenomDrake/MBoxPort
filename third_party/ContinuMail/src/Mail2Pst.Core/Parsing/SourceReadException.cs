// SPDX-License-Identifier: GPL-3.0-or-later
#nullable enable
using System;
using System.IO;
using Mail2Pst.Core.Reporting;

namespace Mail2Pst.Core.Parsing;

/// <summary>A source could not be read completely. This is a run failure, not one skipped message.</summary>
public sealed class SourceReadException : IOException
{
    public string SourcePath { get; }

    /// <summary>Partial results after writer cleanup; null if reading failed before writing started.</summary>
    public ConversionReport? Report { get; internal set; }

    public SourceReadException(string sourcePath, Exception innerException)
        : base($"Failed to read source '{sourcePath}' completely. The conversion is incomplete: {innerException.Message}", innerException)
    {
        SourcePath = sourcePath;
    }
}
