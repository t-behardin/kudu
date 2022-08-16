using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Kudu.Contracts.Infrastructure;

namespace Kudu.Core.Diagnostics
{
    [DebuggerDisplay("{Id} {Name}")]
    public class ProcessInfo : INamedObject
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonIgnore]
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "to provide ARM specific name")]
        string INamedObject.Name { get { return Id.ToString(); } }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("machineName")]
        public string MachineName { get; set; }

        [JsonPropertyName("href"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Uri Href { get; set; }

        [JsonPropertyName("minidump"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Uri MiniDump { get; set; }

        [JsonPropertyName("is_profile_running"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsProfileRunning { get; set; }

        [JsonPropertyName("is_iis_profile_running"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsIisProfileRunning { get; set; }

        [JsonPropertyName("iis_profile_timeout_in_seconds"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double IisProfileTimeoutInSeconds { get; set; }

        [JsonPropertyName("parent"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Uri Parent { get; set; }

        [JsonPropertyName("children"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public IEnumerable<Uri> Children { get; set; }

        [JsonPropertyName("threads"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public IEnumerable<ProcessThreadInfo> Threads { get; set; }

        [JsonPropertyName("open_file_handles"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public IEnumerable<string> OpenFileHandles { get; set; }

        [JsonPropertyName("modules"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public IEnumerable<ProcessModuleInfo> Modules { get; set; }

        [JsonPropertyName("file_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string FileName { get; set; }

        [JsonPropertyName("command_line"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string CommandLine { get; set; }

        //[JsonProperty(PropertyName = "arguments", DefaultValueHandling = DefaultValueHandling.Ignore)]
        //public string Arguments { get; set; }

        [JsonPropertyName("user_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string UserName { get; set; }

        [JsonPropertyName("handle_count"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int HandleCount { get; set; }

        [JsonPropertyName("module_count"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int ModuleCount { get; set; }

        [JsonPropertyName("thread_count"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int ThreadCount { get; set; }

        [JsonPropertyName("start_time"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("total_cpu_time"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public TimeSpan TotalProcessorTime { get; set; }

        [JsonPropertyName("user_cpu_time"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public TimeSpan UserProcessorTime { get; set; }

        [JsonPropertyName("privileged_cpu_time"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public TimeSpan PrivilegedProcessorTime { get; set; }

        [JsonPropertyName("working_set"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long WorkingSet64 { get; set; }

        [JsonPropertyName("peak_working_set"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long PeakWorkingSet64 { get; set; }

        [JsonPropertyName("private_memory"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long PrivateMemorySize64 { get; set; }

        [JsonPropertyName("virtual_memory"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long VirtualMemorySize64 { get; set; }

        [JsonPropertyName("peak_virtual_memory"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long PeakVirtualMemorySize64 { get; set; }

        [JsonPropertyName("paged_system_memory"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long PagedSystemMemorySize64 { get; set; }

        [JsonPropertyName("non_paged_system_memory"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long NonpagedSystemMemorySize64 { get; set; }

        [JsonPropertyName("paged_memory"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long PagedMemorySize64 { get; set; }

        [JsonPropertyName("peak_paged_memory"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long PeakPagedMemorySize64 { get; set; }

        [JsonPropertyName("time_stamp"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public DateTime TimeStamp { get; set; }

        [JsonPropertyName("environment_variables"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<string, string> EnvironmentVariables { get; set; }

        [JsonPropertyName("is_scm_site"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsScmSite { get; set; }

        [JsonPropertyName("is_webjob"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsWebJob { get; set; }

        [JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Description { get; set; }
    }
}