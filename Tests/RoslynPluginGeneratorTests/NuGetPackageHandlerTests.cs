//-----------------------------------------------------------------------
// <copyright file="NuGetPackageHandlerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System.IO;
using NuGet;
using System.Linq;
using System.Collections.Generic;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    [TestClass]
    public class NuGetPackageHandlerTests
    {
        public TestContext TestContext { get; set; }

        private const string testPackageName = "testPackage";
        private const string dependentPackageName = "dependentPackage";

        private const string releaseVersion = "1.0.0";
        private const string preReleaseVersion = "1.0.0-RC1";

        [TestMethod]
        public void NuGet_TestPackageDownload_Release_Release()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string testDownloadDir = Path.Combine(testDir, "download");

            // Create test NuGet payload and packages
            string testPackageFile = Path.Combine(testDir, "testPackage.nupkg");
            string dependentPackageFile = Path.Combine(testDir, "dependentPackage.nupkg");

            ManifestMetadata testMetadata = new ManifestMetadata()
            {
                Authors = "Microsoft",
                Version = releaseVersion,
                Id = testPackageName,
                Description = "A description",
            };
            buildPackage(testMetadata, testPackageFile);

            List<ManifestDependencySet> dependencies = new List<ManifestDependencySet>()
            {
                new ManifestDependencySet()
                {
                    Dependencies = new List<ManifestDependency>()
                    {
                        new ManifestDependency()
                        {
                            Id = testPackageName,
                            Version = releaseVersion,
                        }
                    }
                }
            };
            ManifestMetadata dependentMetadata = new ManifestMetadata()
            {
                Authors = "Microsoft",
                Version = releaseVersion,
                Id = dependentPackageName,
                Description = "A description",
                DependencySets = dependencies,
            };
            buildPackage(dependentMetadata, dependentPackageFile);

            TestLogger logger = new TestLogger();
            NuGetPackageHandler handler = new NuGetPackageHandler(logger);

            // Act
            // Attempt to download a package which is released with a dependency that is released
            IPackage package = handler.FetchPackage(testDir, dependentPackageName, null, testDownloadDir);

            // Assert
            // Package should have been downloaded
            AssertPackageDownloaded(testDownloadDir, dependentPackageName);
            Assert.IsNotNull(package, "Expected a reference to the package to be returned");
        }

        [TestMethod]
        public void NuGet_TestPackageDownload_PreRelease_Release()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string testDownloadDir = Path.Combine(testDir, "download");

            // Create test NuGet payload and packages
            string testPackageFile = Path.Combine(testDir, "testPackage.nupkg");
            string dependentPackageFile = Path.Combine(testDir, "dependentPackage.nupkg");

            ManifestMetadata testMetadata = new ManifestMetadata()
            {
                Authors = "Microsoft",
                Version = releaseVersion,
                Id = testPackageName,
                Description = "A description",
            };
            buildPackage(testMetadata, testPackageFile);

            List<ManifestDependencySet> dependencies = new List<ManifestDependencySet>()
            {
                new ManifestDependencySet()
                {
                    Dependencies = new List<ManifestDependency>()
                    {
                        new ManifestDependency()
                        {
                            Id = testPackageName,
                            Version = releaseVersion,
                        }
                    }
                }
            };
            ManifestMetadata dependentMetadata = new ManifestMetadata()
            {
                Authors = "Microsoft",
                Version = preReleaseVersion,
                Id = dependentPackageName,
                Description = "A description",
                DependencySets = dependencies,
            };
            buildPackage(dependentMetadata, dependentPackageFile);

            TestLogger logger = new TestLogger();
            NuGetPackageHandler handler = new NuGetPackageHandler(logger);

            // Act
            // Attempt to download a package which is not released with a dependency that is released
            IPackage package = handler.FetchPackage(testDir, dependentPackageName, null, testDownloadDir);

            // Assert
            // Package should have been downloaded
            AssertPackageDownloaded(testDownloadDir, dependentPackageName);
            Assert.IsNotNull(package, "Expected a reference to the package to be returned");
        }

        [TestMethod]
        public void NuGet_TestPackageDownload_PreRelease_PreRelease()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string testDownloadDir = Path.Combine(testDir, "download");

            // Create test NuGet payload and packages
            string testPackageFile = Path.Combine(testDir, "testPackage.nupkg");
            string dependentPackageFile = Path.Combine(testDir, "dependentPackage.nupkg");

            ManifestMetadata testMetadata = new ManifestMetadata()
            {
                Authors = "Microsoft",
                Version = preReleaseVersion,
                Id = testPackageName,
                Description = "A description",
            };
            buildPackage(testMetadata, testPackageFile);

            List<ManifestDependencySet> dependencies = new List<ManifestDependencySet>()
            {
                new ManifestDependencySet()
                {
                    Dependencies = new List<ManifestDependency>()
                    {
                        new ManifestDependency()
                        {
                            Id = testPackageName,
                            Version = preReleaseVersion,
                        }
                    }
                }
            };
            ManifestMetadata dependentMetadata = new ManifestMetadata()
            {
                Authors = "Microsoft",
                Version = preReleaseVersion,
                Id = dependentPackageName,
                Description = "A description",
                DependencySets = dependencies,
            };
            buildPackage(dependentMetadata, dependentPackageFile);

            TestLogger logger = new TestLogger();
            NuGetPackageHandler handler = new NuGetPackageHandler(logger);

            // Act
            // Attempt to download a package which is not released with a dependency that is not released
            IPackage package = handler.FetchPackage(testDir, dependentPackageName, null, testDownloadDir);

            // Assert
            // Package should have been downloaded
            AssertPackageDownloaded(testDownloadDir, dependentPackageName);
            Assert.IsNotNull(package, "Expected a reference to the package to be returned");
        }

        #region Private Methods

        private void AssertPackageDownloaded(string downloadDir, string packageName)
        {
            Assert.IsNotNull(Directory.GetDirectories(downloadDir).SingleOrDefault(d => d.Contains(packageName)),
                "Expected a package to have been downloaded: " + packageName);
        }

        private void buildPackage(ManifestMetadata metadata, string destinationFile)
        {
            string testDir = TestUtils.EnsureTestDirectoryExists(this.TestContext, "source");
            string dummyTextFile = TestUtils.CreateTextFile(Guid.NewGuid().ToString(), testDir, "content");

            PackageBuilder packageBuilder = new PackageBuilder();

            PhysicalPackageFile file = new PhysicalPackageFile();
            file.SourcePath = dummyTextFile;
            file.TargetPath = "dummy.txt";
            packageBuilder.Files.Add(file);
            
            packageBuilder.Populate(metadata);

            using (FileStream stream = File.Open(destinationFile, FileMode.OpenOrCreate))
            {
                packageBuilder.Save(stream);
            }
        }

        #endregion
    }
}
