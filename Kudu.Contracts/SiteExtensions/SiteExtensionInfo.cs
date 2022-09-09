﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;
using Kudu.Contracts.Infrastructure;
using NuGet.Client.VisualStudio;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace Kudu.Contracts.SiteExtensions
{
    // This is equivalent to NuGet.IPackage
    public class SiteExtensionInfo : INamedObject
    {
        public enum SiteExtensionType
        {
            Gallery,
            WebRoot
        }

        public SiteExtensionInfo()
        {
        }

        public SiteExtensionInfo(XElement element)
        {
            var elements = element.Elements();
            Id = GetElementValue(elements, "Id");
            Version = GetElementValue(elements, "Version");
            Title = GetElementValue(elements, "Title");
            Description = GetElementValue(elements, "Description");
            Authors = GetElementValue(elements, "Authors")?.Split(',').Select(n => n.Trim());
            ProjectUrl = GetElementValue(elements, "ProjectUrl");
            LicenseUrl = GetElementValue(elements, "LicenseUrl");
            Summary = GetElementValue(elements, "Summary");
            IconUrl = GetElementValue(elements, "IconUrl") ?? "https://www.nuget.org/Content/Images/packageDefaultIcon-50x50.png";

            // this is to maintain the same behavior as V1
            //if (int.TryParse(GetElementValue(elements, "DownloadCount"), out int downloadCount))
            //{
            //    DownloadCount = downloadCount;
            //}
            //if (DateTime.TryParse(GetElementValue(elements, "Published"), out DateTime publishedDateTime))
            //{
            //    PublishedDateTime = publishedDateTime;
            //}
        }

        public SiteExtensionInfo(JsonNode json)
        {
            Id = (string)json["id"];
            Version = (string)json["version"];
            Title = (string)json["title"];
            Description = (string)json["description"];
            Authors = ((string)json["authors"])?.Split(',').Select(n => n.Trim());
            ProjectUrl = (string)json["projectUrl"];
            LicenseUrl = (string)json["licenseUrl"];
            Summary = (string)json["description"];
            IconUrl = (string)json["iconUrl"] ?? "https://www.nuget.org/Content/Images/packageDefaultIcon-50x50.png";
            PackageUri = (string)json["packageUri"];
        }

        public SiteExtensionInfo(SiteExtensionInfo info)
        {
            Id = info.Id;
            Title = info.Title;
            Type = info.Type;
            Summary = info.Summary;
            Description = info.Description;
            Version = info.Version;
            ExtensionUrl = info.ExtensionUrl;
            ProjectUrl = info.ProjectUrl;
            IconUrl = info.IconUrl;
            LicenseUrl = info.LicenseUrl;
            Authors = info.Authors;
            PublishedDateTime = info.PublishedDateTime;
            IsLatestVersion = info.IsLatestVersion;
            DownloadCount = info.DownloadCount;
            LocalIsLatestVersion = info.LocalIsLatestVersion;
            LocalPath = info.LocalPath;
            InstalledDateTime = info.InstalledDateTime;
            InstallationArgs = info.InstallationArgs;
            PackageUri = info.PackageUri;
        }

        public SiteExtensionInfo(UIPackageMetadata data)
        {
            Id = data.Identity.Id;
            Title = data.Title;
            Type = SiteExtensionType.Gallery;
            Summary = data.Summary;
            Description = data.Description;
            Version = data.Identity.Version.ToNormalizedString();
            ProjectUrl = data.ProjectUrl == null ? null : data.ProjectUrl.ToString();
            IconUrl = data.IconUrl == null ? "https://www.nuget.org/Content/Images/packageDefaultIcon-50x50.png" : data.IconUrl.ToString();
            LicenseUrl = data.LicenseUrl == null ? null : data.LicenseUrl.ToString();
            Authors = data.Authors.Split(new string[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
            PublishedDateTime = data.Published;
            IsLatestVersion = true;
            DownloadCount = data.DownloadCount;
        }

        [JsonPropertyName("id")]
        public string Id
        {
            get;
            set;
        }

        [JsonPropertyName("title")]
        public string Title
        {
            get;
            set;
        }

        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SiteExtensionType Type
        {
            get;
            set;
        }

        [JsonPropertyName("summary")]
        public string Summary
        {
            get;
            set;
        }

        [JsonPropertyName("description")]
        public string Description
        {
            get;
            set;
        }

        [JsonPropertyName("version")]
        public string Version
        {
            get;
            set;
        }

        [JsonPropertyName("extension_url")]
        public string ExtensionUrl
        {
            get;
            set;
        }

        [JsonPropertyName("project_url")]
        public string ProjectUrl
        {
            get;
            set;
        }

        [JsonPropertyName("icon_url")]
        public string IconUrl
        {
            get;
            set;
        }

        [JsonPropertyName("license_url")]
        public string LicenseUrl
        {
            get;
            set;
        }

        [JsonPropertyName("feed_url")]
        public string FeedUrl
        {
            get;
            set;
        }

        [JsonPropertyName("authors")]
        public IEnumerable<string> Authors
        {
            get;
            set;
        }

        [JsonPropertyName("installer_command_line_params")]
        public string InstallationArgs
        {
            get;
            set;
        }

        [JsonPropertyName("published_date_time")]
        public DateTimeOffset? PublishedDateTime
        {
            get;
            set;
        }

        [JsonIgnore]
        public bool IsLatestVersion
        {
            get;
            set;
        }

        [JsonPropertyName("download_count")]
        public int DownloadCount
        {
            get;
            set;
        }

        [JsonPropertyName("local_is_latest_version")]
        public bool? LocalIsLatestVersion
        {
            get;
            set;
        }

        [JsonPropertyName("local_path")]
        public string LocalPath
        {
            get;
            set;
        }

        [JsonPropertyName("installed_date_time")]
        public DateTimeOffset? InstalledDateTime
        {
            get;
            set;
        }

        // For Arm Request
        [JsonIgnore]
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "to provide ARM specific name")]
        string INamedObject.Name { get { return Id; } }

        [JsonPropertyName("provisioningState")]
        public string ProvisioningState
        {
            get;
            set;
        }

        [JsonPropertyName("comment")]
        public string Comment
        {
            get;
            set;
        }

        /// <summary>
        /// explicit url to download nupkg such as https://globalcdn.nuget.org/packages/microsoft.applicationinsights.azurewebsites.2.6.5.nupkg
        /// </summary>
        [JsonPropertyName("packageUri")]
        public string PackageUri
        {
            get;
            set;
        }

        private static string GetElementValue(IEnumerable<XElement> elements, string name)
        {
            var element = elements.FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (element == null)
            {
                return null;
            }

            var value = element.Value;
            if (string.IsNullOrEmpty(value) && element.Attributes().Any(a => a.Name.LocalName.Equals("null", StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            return value;
        }
    }
}
