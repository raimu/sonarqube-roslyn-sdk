/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.IO.Compression;

namespace SonarQube.Plugins.Test.Common
{
    /// <summary>
    /// Utility class used to check the file content of zipped files, which could be jar files
    /// </summary>
    public class ZipFileChecker
    {
        private readonly string unzippedDir;
        private readonly TestContext testContext;

        public ZipFileChecker(TestContext testContext, string zipFilePath)
        {
            this.testContext = testContext;
            TestUtils.AssertFileExists(zipFilePath);

            this.unzippedDir = TestUtils.CreateTestDirectory(testContext, "unzipped." + Path.GetFileNameWithoutExtension(zipFilePath));
            ZipFile.ExtractToDirectory(zipFilePath, this.unzippedDir);
        }

        /// <summary>
        /// Returns the folder into which the zip was unpacked
        /// </summary>
        public string UnzippedDirectoryPath {  get { return this.unzippedDir; } }

        public string AssertFileExists(string relativeFilePath)
        {
            string absolutePath = Path.Combine(this.unzippedDir, relativeFilePath);
            Assert.IsTrue(File.Exists(absolutePath), "File does not exist in the zip: {0}", relativeFilePath);
            return absolutePath;
        }

        public void AssertZipContainsFiles(params string[] expectedRelativePaths)
        {
            foreach (string relativePath in expectedRelativePaths)
            {
                this.testContext.WriteLine("ZipFileChecker: checking for file '{0}'", relativePath);

                string[] matchingFiles = Directory.GetFiles(this.unzippedDir, relativePath, SearchOption.TopDirectoryOnly);

                Assert.IsTrue(matchingFiles.Length < 2, "Test error: supplied relative path should not match multiple files");
                Assert.AreEqual(1, matchingFiles.Length, "Zip file does not contain expected file: {0}", relativePath);

                this.testContext.WriteLine("ZipFileChecker: found at '{0}'", matchingFiles[0]);
            }
        }

        public void AssertZipContainsOnlyExpectedFiles(params string[] expectedRelativePaths)
        {
            AssertZipContainsFiles(expectedRelativePaths);

            string[] allFilesInZip = Directory.GetFiles(this.unzippedDir, "*.*", SearchOption.AllDirectories);
            Assert.AreEqual(expectedRelativePaths.Length, allFilesInZip.Length, "Zip contains more files than expected");
        }

    }
}
