﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kudu.Contracts.Tracing;
using Kudu.Services.Editor;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Moq;
using Xunit;

namespace Kudu.Core.Test
{
    public class FileSystemHubFacts
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task FileSystemHubRegisterTests(bool update)
        {
            // Setup
            var env = new Mock<IEnvironment>();
            var tracer = new Mock<ITracer>();
            var context = new HubCallerContext(Mock.Of<IRequest>(), Guid.NewGuid().ToString());
            var groups = new Mock<IGroupManager>();
            var clients = new Mock<HubConnectionContext>();
            using (
                var fileSystemHubTest = new FileSystemHubTest(env.Object, tracer.Object, context, groups.Object,
                    clients.Object))
            {
                // Test
                fileSystemHubTest.TestRegister(GetEmptyDir());

                // Assert
                Assert.Equal(1, FileSystemHubTest.FileWatchersCount);

                if (update)
                {
                    // Test
                    fileSystemHubTest.TestRegister(GetEmptyDir());

                    // Assert
                    Assert.Equal(1, FileSystemHubTest.FileWatchersCount);
                }

                // Test
                await fileSystemHubTest.TestDisconnect();

                // Assert
                Assert.Equal(0, FileSystemHubTest.FileWatchersCount);
            }
        }

        [Fact]
        public async Task FileSystemHubDisconnectTest()
        {
            // Setup
            var env = new Mock<IEnvironment>();
            var tracer = new Mock<ITracer>();
            var context = new HubCallerContext(Mock.Of<IRequest>(), Guid.NewGuid().ToString());
            var groups = new Mock<IGroupManager>();
            var clients = new Mock<HubConnectionContext>();

            using (
                var fileSystemHubTest = new FileSystemHubTest(env.Object, tracer.Object, context, groups.Object,
                    clients.Object))
            {
                // Test
                fileSystemHubTest.TestRegister(GetEmptyDir());

                // Assert
                Assert.Equal(1, FileSystemHubTest.FileWatchersCount);

                // Test
                await fileSystemHubTest.TestDisconnect();
            }
            // Assert
            Assert.Equal(0, FileSystemHubTest.FileWatchersCount);
        }

        [Fact]
        public void FileSystemHubMaxWatchers()
        {
            var listOfFileSystemHubs = new List<FileSystemHubTest>();

            try
            {
                // Setup
                var env = new Mock<IEnvironment>();
                var tracer = new Mock<ITracer>();
                var groups = new Mock<IGroupManager>();
                var clients = new Mock<HubConnectionContext>();

                // Test
                for (var i = 0; i < 10; i++)
                {
                    var context = new HubCallerContext(Mock.Of<IRequest>(), Guid.NewGuid().ToString());
                    var fileSystemHubTest = new FileSystemHubTest(env.Object, tracer.Object, context, groups.Object,
                        clients.Object);
                    listOfFileSystemHubs.Add(fileSystemHubTest);
                    fileSystemHubTest.TestRegister(GetEmptyDir());

                    // Assert
                    Assert.Equal(Math.Min(i + 1, FileSystemHub.MaxFileSystemWatchers), FileSystemHubTest.FileWatchersCount);
                }
            }
            finally
            {
                foreach (var fileSystemHub in listOfFileSystemHubs)
                {
                    fileSystemHub.Dispose();
                }
            }
        }

        private string GetEmptyDir()
        {
            var dir = Path.GetTempPath();
            var info = Directory.CreateDirectory(Guid.NewGuid().ToString());
            return info.FullName;
        }
    }

    public class FileSystemHubTest : FileSystemHub, IDisposable
    {
        public FileSystemHubTest(IEnvironment environment, ITracer tracer, HubCallerContext context,
            IGroupManager group, HubConnectionContext clients) : base(environment, tracer)
        {
            Context = context;
            Groups = group;
            Clients = clients;
        }

        public static int FileWatchersCount
        {
            get { return _fileWatchers.Count; }
        }

        public void TestRegister(string path)
        {
            Register(path);
        }

        public Task TestDisconnect()
        {
            return OnDisconnected(stopCalled: false);
        }

        public new void Dispose()
        {
            _fileWatchers.Clear();
        }
    }
}
