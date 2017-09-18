﻿/*
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

using SonarQube.Plugins.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SonarQube.Common
{
    /// <summary>
    /// Helper class to run an executable and capture the output
    /// </summary>
    public sealed class ProcessRunner
    {
        public const int ErrorCode = 1;

        private ILogger outputLogger;

        #region Public methods

        public bool ErrorsLogged { get; private set; }

        public int ExitCode { get; private set; }

        /// <summary>
        /// Runs the specified executable and returns a boolean indicating success or failure
        /// </summary>
        /// <remarks>The standard and error output will be streamed to the logger. Child processes do not inherit the env variables from the parent automatically</remarks>
        public bool Execute(ProcessRunnerArguments runnerArgs)
        {
            if (runnerArgs == null)
            {
                throw new ArgumentNullException("runnerArgs");
            }
            Debug.Assert(!string.IsNullOrWhiteSpace(runnerArgs.ExeName), "Process runner exe name should not be null/empty");
            Debug.Assert( runnerArgs.Logger!= null, "Process runner logger should not be null/empty");

            this.outputLogger = runnerArgs.Logger;

            if (!File.Exists(runnerArgs.ExeName))
            {
                this.outputLogger.LogError(Resources.ERROR_ProcessRunner_ExeNotFound, runnerArgs.ExeName);
                this.ExitCode = ErrorCode;
                return false;
            }

            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = runnerArgs.ExeName,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false, // required if we want to capture the error output
                ErrorDialog = false,
                CreateNoWindow = true,
                Arguments = runnerArgs.GetQuotedCommandLineArgs(),
                WorkingDirectory = runnerArgs.WorkingDirectory
            };

            SetEnvironmentVariables(psi, runnerArgs.EnvironmentVariables, runnerArgs.Logger);

            bool succeeded;
            Process process = null;
            try
            {
                process = new Process();

                process.StartInfo = psi;
                process.ErrorDataReceived += OnErrorDataReceived;
                process.OutputDataReceived += OnOutputDataReceived;

                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                // Warning: do not log the raw command line args as they
                // may contain sensitive data
                this.outputLogger.LogDebug(Resources.MSG_ExecutingFile,
                    runnerArgs.ExeName,
                    runnerArgs.GetCommandLineArgsLogText(),
                    runnerArgs.WorkingDirectory,
                    runnerArgs.TimeoutInMilliseconds,
                    process.Id);

                succeeded = process.WaitForExit(runnerArgs.TimeoutInMilliseconds);
                if (succeeded)
                {
                    process.WaitForExit(); // Give any asynchronous events the chance to complete
                }

                // false means we asked the process to stop but it didn't.
                // true: we might still have timed out, but the process ended when we asked it to
                if (succeeded)
                {
                    this.outputLogger.LogDebug(Resources.MSG_ExecutionExitCode, process.ExitCode);
                    this.ExitCode = process.ExitCode;
                }
                else
                {
                    this.ExitCode = ErrorCode;
                    this.outputLogger.LogWarning(Resources.WARN_ExecutionTimedOut, runnerArgs.TimeoutInMilliseconds, runnerArgs.ExeName);
                }

                succeeded = succeeded && (this.ExitCode == 0);
            }
            finally
            {
                process.ErrorDataReceived -= OnErrorDataReceived;
                process.OutputDataReceived -= OnOutputDataReceived;

                process.Dispose();
            }
            return succeeded;
        }

        #endregion

        #region Private methods

        private static void SetEnvironmentVariables(ProcessStartInfo psi, IDictionary<string, string> envVariables, ILogger logger)
        {
            if (envVariables == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> envVariable in envVariables)
            {
                Debug.Assert(!String.IsNullOrEmpty(envVariable.Key), "Env variable name cannot be null or empty");

                if (psi.EnvironmentVariables.ContainsKey(envVariable.Key))
                {
                    logger.LogDebug(Resources.MSG_Runner_OverwritingEnvVar, envVariable.Key, psi.EnvironmentVariables[envVariable.Key], envVariable.Value);
                }
                else
                {
                    logger.LogDebug(Resources.MSG_Runner_SettingEnvVar, envVariable.Key, envVariable.Value);
                }
                psi.EnvironmentVariables[envVariable.Key] = envVariable.Value;
            }
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                // It's important to log this as an important message because
                // this the log redirection pipeline of the child process
                this.outputLogger.LogInfo(e.Data);
            }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                this.ErrorsLogged = true;
                this.outputLogger.LogError(e.Data);
            }
        }

        #endregion
    }
}
