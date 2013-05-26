// Guids.cs
// MUST match guids.h
using System;

namespace ThomasLevesque.AutoRunCustomTool
{
    static class GuidList
    {
        public const string guidAutoRunCustomToolPkgString = "7ec0f89c-f00d-48ab-a76b-713d90fdbf03";
        public const string guidAutoRunCustomToolCmdSetString = "0c95b506-373b-43f9-b98b-0fa389e08a6f";

        public static readonly Guid guidAutoRunCustomToolCmdSet = new Guid(guidAutoRunCustomToolCmdSetString);
    };
}