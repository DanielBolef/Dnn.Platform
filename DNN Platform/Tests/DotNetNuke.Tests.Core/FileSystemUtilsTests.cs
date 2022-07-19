﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

namespace DotNetNuke.Tests.Core
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;

    using DotNetNuke.Abstractions;
    using DotNetNuke.Abstractions.Application;
    using DotNetNuke.Common;
    using DotNetNuke.Common.Utilities;
    using DotNetNuke.ComponentModel;
    using DotNetNuke.Entities.Tabs;
    using DotNetNuke.Tests.Utilities.Mocks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;

    /// <summary>
    ///   FileSystemUtilsTests.
    /// </summary>
    [TestFixture]
    public class FileSystemUtilsTests
    {
        [SetUp]
        public void SetUp()
        {
            var applicationStatusInfo = new DotNetNuke.Application.ApplicationStatusInfo(Mock.Of<IApplicationInfo>());
            var rootPath = Path.Combine(applicationStatusInfo.ApplicationMapPath, "FileSystemUtilsTest");
            this.PrepareRootPath(rootPath, applicationStatusInfo.ApplicationMapPath);

            var serviceCollection = new ServiceCollection();
            var mock = new Mock<IApplicationStatusInfo>();
            mock.Setup(info => info.ApplicationMapPath).Returns(rootPath);
            serviceCollection.AddTransient<IApplicationStatusInfo>(container => mock.Object);
            serviceCollection.AddTransient<INavigationManager>(container => Mock.Of<INavigationManager>());
            Globals.DependencyProvider = serviceCollection.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            Globals.DependencyProvider = null;
        }

        [TestCase("/")]
        [TestCase("//")]
        [TestCase("///")]
        [TestCase("\\")]
        [TestCase("\\\\")]
        [TestCase("\\\\\\")]
        [TestCase("/Test/../")]
        [TestCase("/Test/mmm/../../")]
        [TestCase("\\Test\\..\\")]
        [TestCase("\\Test\\mmm\\..\\..\\")]
        [TestCase("\\Test\\")]
        [TestCase("..\\")]
        public void DeleteFiles_Should_Not_Able_To_Delete_Root_Folder(string path)
        {
            // Action
            FileSystemUtils.DeleteFiles(new string[] { path });

            var files = Directory.GetFiles(Globals.ApplicationMapPath, "*.*", SearchOption.AllDirectories);
            Assert.Greater(files.Length, 0);
        }

        [Test]
        public void AddToZip_Should_Able_To_Add_Multiple_Files()
        {
            // Action
            this.DeleteZippedFiles();
            var zipFilePath = Path.Combine(Globals.ApplicationMapPath, $"Test{Guid.NewGuid().ToString().Substring(0, 8)}.zip");
            var files = Directory.GetFiles(Globals.ApplicationMapPath, "*.*", SearchOption.TopDirectoryOnly);
            using (var stream = File.Create(zipFilePath))
            {
                var zipStream = new ZipArchive(stream, ZipArchiveMode.Create, true);

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    FileSystemUtils.AddToZip(ref zipStream, file, fileName, string.Empty);
                }

                zipStream.Dispose();
            }

            // Assert
            var destPath = Path.Combine(Globals.ApplicationMapPath, Path.GetFileNameWithoutExtension(zipFilePath));
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            try
            {
                using (var stream = File.OpenRead(zipFilePath))
                {
                    var zipStream = new ZipArchive(stream, ZipArchiveMode.Read, true);
                    FileSystemUtils.UnzipResources(zipStream, destPath);
                    zipStream.Dispose();
                }

                var unZippedFiles = Directory.GetFiles(destPath, "*.*", SearchOption.TopDirectoryOnly);
                Assert.AreEqual(files.Length, unZippedFiles.Length);
            }
            finally
            {
                this.DeleteZippedFiles();
                this.DeleteUnzippedFolder(destPath);
            }
        }

        [Test]
        public void DeleteFile_Should_Delete_File()
        {
            // Action
            var testPath = Globals.ApplicationMapPath + $"/Test{Guid.NewGuid().ToString().Substring(0, 8)}.txt";
            using (StreamWriter sw = File.CreateText(testPath))
            {
                sw.WriteLine("48");
            }

            FileSystemUtils.DeleteFile(testPath);

            // Assert
            bool res = File.Exists(testPath.Replace("/", "\\"));
            Assert.IsFalse(res);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("/")]
        [TestCase("Test/Test ")]
        public void FixPath_Should_Change_Slashes_And_Trim(string input)
        {
            // Action
            var result = FileSystemUtils.FixPath(input);

            // Assert
            if (string.IsNullOrEmpty(input))
            {
                Assert.IsTrue(input == result);
            }
            else if (string.IsNullOrWhiteSpace(input))
            {
                Assert.IsTrue(result == string.Empty);
            }
            else
            {
                Assert.IsFalse(result.Contains(" "));
                Assert.IsFalse(result.Contains("/"));
            }
        }

        private void PrepareRootPath(string rootPath, string applicationMapPath)
        {
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            foreach (var file in Directory.GetFiles(applicationMapPath, "*.*", SearchOption.TopDirectoryOnly))
            {
                File.Copy(file, Path.Combine(rootPath, Path.GetFileName(file)), true);
            }
        }

        private void DeleteZippedFiles()
        {
            var excludedFiles = Directory.GetFiles(Globals.ApplicationMapPath, "Test*", SearchOption.TopDirectoryOnly);
            foreach (var f in excludedFiles)
            {
                try
                {
                    File.Delete(f);
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        private void DeleteUnzippedFolder(string zippedFolder)
        {
            try
            {
                Directory.Delete(zippedFolder, true);
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }
}
