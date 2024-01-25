using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AlarmClock
{
    // https://jackma.com/2019/04/20/execute-a-bash-script-via-c-net-core/
    public static class ShellHelper
    {
        public static Task<int> Bash(this string cmd, ILogger logger)
        {
            var source = new TaskCompletionSource<int>();
            var escapedArgs = cmd.Replace("\"", "\\\"");
            var process = new Process
                            {
                            StartInfo = new ProcessStartInfo
                                            {
                                            FileName = "bash",
                                            Arguments = $"-c \"{escapedArgs}\"",
                                            RedirectStandardOutput = true,
                                            RedirectStandardError = true,
                                            UseShellExecute = false,
                                            CreateNoWindow = true
                                            },
                            EnableRaisingEvents = true,
                            };
            process.Exited += (sender, args) =>
            {
                if (logger != null)
                {
                    logger.LogWarning(process.StandardError.ReadToEnd());
                    logger.LogInformation(process.StandardOutput.ReadToEnd());
                }
                if (process.ExitCode == 0)
                {
                    source.SetResult(0);
                }
                else
                {
                    source.SetException(new Exception($"Command `{cmd}` failed with exit code `{process.ExitCode}`"));
                }
                process.Dispose();
            };

            try
            {
                process.Start();

                // Wait upto 3 seconds to complete.
                process.WaitForExit(TimeSpan.FromSeconds(5));
            }
            catch (Exception e)
            {
                if (logger != null)
                    logger.LogError(e, $"Command '{cmd}' failed");
                source.SetException(e);
            }

            return source.Task;
        }
    }
  }
