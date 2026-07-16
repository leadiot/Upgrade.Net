using Com.Scm.Upgrade.Config;
using System.IO.Compression;

namespace Com.Scm.Upgrade
{
    public class Upgrade
    {
        public const int MAJOR = 1;
        public const int MINOR = 1;
        public const int PATCH = 2;
        public const int BUILD = 2;
        public const string RELEASE = "2026-07-16";

        private static readonly HttpClient _HttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        private readonly Dictionary<UpgradeOption, UpgradeAction> _Actions = new Dictionary<UpgradeOption, UpgradeAction>();
        private StreamWriter _writer;

        private UpgradeView _View;
        private UpgradeConfig _Config;

        public Upgrade(UpgradeView view)
        {
            _View = view;

            InitializeActions();
        }

        private void InitializeActions()
        {
            _Actions[UpgradeOption.Download] = new UpgradeAction
            {
                Option = UpgradeOption.Download,
                Title = "下载文件",
                Description = "从指定URL下载文件",
                Execute = ExecuteDownload
            };

            _Actions[UpgradeOption.Upload] = new UpgradeAction
            {
                Option = UpgradeOption.Upload,
                Title = "上传文件",
                Description = "将本地文件上传到指定URL",
                Execute = ExecuteUpload
            };

            _Actions[UpgradeOption.Command] = new UpgradeAction
            {
                Option = UpgradeOption.Command,
                Title = "执行命令",
                Description = "执行指定的系统命令，等待命令执行完成",
                Execute = ExecuteCommand
            };

            _Actions[UpgradeOption.Launch] = new UpgradeAction
            {
                Option = UpgradeOption.Launch,
                Title = "启动程序",
                Description = "启动外部程序，不等待执行完成",
                Execute = ExecuteLaunch
            };

            _Actions[UpgradeOption.Zip] = new UpgradeAction
            {
                Option = UpgradeOption.Zip,
                Title = "压缩文件",
                Description = "将指定目录或文件压缩为ZIP",
                Execute = ExecuteZip
            };

            _Actions[UpgradeOption.Unzip] = new UpgradeAction
            {
                Option = UpgradeOption.Unzip,
                Title = "解压文件",
                Description = "将ZIP文件解压到指定目录",
                Execute = ExecuteUnzip
            };

            _Actions[UpgradeOption.MoveDir] = new UpgradeAction
            {
                Option = UpgradeOption.MoveDir,
                Title = "移动目录",
                Description = "将目录移动到指定位置",
                Execute = ExecuteMoveDir
            };

            _Actions[UpgradeOption.MoveDoc] = new UpgradeAction
            {
                Option = UpgradeOption.MoveDoc,
                Title = "移动文件",
                Description = "将文件移动到指定位置",
                Execute = ExecuteMoveDoc
            };

            _Actions[UpgradeOption.CopyDir] = new UpgradeAction
            {
                Option = UpgradeOption.CopyDir,
                Title = "复制目录",
                Description = "将目录复制到指定位置",
                Execute = ExecuteCopyDir
            };

            _Actions[UpgradeOption.CopyDoc] = new UpgradeAction
            {
                Option = UpgradeOption.CopyDoc,
                Title = "复制文件",
                Description = "将文件复制到指定位置",
                Execute = ExecuteCopyDoc
            };

            _Actions[UpgradeOption.CreateDir] = new UpgradeAction
            {
                Option = UpgradeOption.CreateDir,
                Title = "创建目录",
                Description = "创建指定目录",
                Execute = ExecuteCreateDir
            };

            _Actions[UpgradeOption.CreateDoc] = new UpgradeAction
            {
                Option = UpgradeOption.CreateDoc,
                Title = "创建文件",
                Description = "创建指定文件",
                Execute = ExecuteCreateDoc
            };

            _Actions[UpgradeOption.DeleteDir] = new UpgradeAction
            {
                Option = UpgradeOption.DeleteDir,
                Title = "删除目录",
                Description = "删除指定目录",
                Execute = ExecuteDeleteDir
            };

            _Actions[UpgradeOption.DeleteDoc] = new UpgradeAction
            {
                Option = UpgradeOption.DeleteDoc,
                Title = "删除文件",
                Description = "删除指定文件",
                Execute = ExecuteDeleteDoc
            };

            _Actions[UpgradeOption.RenameDir] = new UpgradeAction
            {
                Option = UpgradeOption.RenameDir,
                Title = "更名目录",
                Description = "更名指定目录",
                Execute = ExecuteRenameDir
            };

            _Actions[UpgradeOption.RenameDoc] = new UpgradeAction
            {
                Option = UpgradeOption.RenameDoc,
                Title = "更名文件",
                Description = "更名指定文件",
                Execute = ExecuteRenameDoc
            };
        }

        /// <summary>
        /// 执行升级（异步）
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task StartAsync(UpgradeConfig config)
        {
            _Config = config;

            if (config.Steps == null || config.Steps.Count < 1)
            {
                Log("[警告] 未配置升级步骤，请在 upgrade.json 中配置 Steps");
                return;
            }

            await ExecuteStepsAsync(config);
        }

        /// <summary>
        /// 执行升级（同步）
        /// </summary>
        /// <param name="config"></param>
        public void Start(UpgradeConfig config)
        {
            StartAsync(config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 执行升级步骤
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task ExecuteStepsAsync(UpgradeConfig config)
        {
            Log("────────────────────────────────────────");
            Log($"升级程序：v{MAJOR}.{MINOR}.{PATCH}.{BUILD} ({RELEASE})");
            Log($"开始执行：{config.Steps.Count} 个升级步骤...");
            Log("────────────────────────────────────────");
            LogNewLine();

            var failedSteps = new List<int>();

            for (int i = 0; i < config.Steps.Count; i++)
            {
                var step = config.Steps[i];
                var stepNumber = i + 1;

                _View?.ResetProgress();

                var action = GetAction(step.Option);
                if (action == null)
                {
                    LogStep(stepNumber, config.Steps.Count, $"跳过未知操作类型：{step.Option}");
                    LogStepStatus(stepNumber, StepStatus.Skipped, "跳过未知操作");
                    continue;
                }

                var title = string.IsNullOrEmpty(step.Title) ? action.Title : step.Title;
                var description = string.IsNullOrEmpty(step.Description) ? action.Description : step.Description;

                LogStep(stepNumber, config.Steps.Count, $"正在执行：{title}");
                LogStepInfo(stepNumber, "说明", description);

                if (!string.IsNullOrEmpty(step.Description))
                {
                    LogStepInfo(stepNumber, "状态", "准备执行...");
                }

                LogStepStatus(stepNumber, StepStatus.Running, title + "执行中");

                var result = await ExecuteStepWithRetryAsync(step, action, stepNumber);

                if (!result.Success)
                {
                    failedSteps.Add(stepNumber);

                    LogStepStatus(stepNumber, StepStatus.Failed, result.Message);

                    if (!step.ContinueOnError)
                    {
                        LogStepInfo(stepNumber, "终止", $"步骤失败且不允许继续，终止升级流程");
                        throw new Exception($"步骤 {stepNumber} 执行失败：{result.Message}");
                    }

                    LogStepInfo(stepNumber, "继续", $"步骤失败但允许继续，继续执行后续步骤");
                }
                else
                {
                    LogStepInfo(stepNumber, "完成", result.Message);

                    if (step.WaitTime > 0)
                    {
                        LogStepWait(stepNumber, step.WaitTime, $"等待 {step.WaitTime} 秒后继续...");
                        for (int remaining = step.WaitTime; remaining > 0; remaining--)
                        {
                            LogStepWait(stepNumber, remaining, $"等待中，剩余 {remaining} 秒");
                            await Task.Delay(1000);
                        }
                        LogStepWait(stepNumber, 0, "等待结束，继续下一步");
                    }

                    LogStepStatus(stepNumber, StepStatus.Success, result.Message);
                }

                LogNewLine();
            }

            Log("────────────────────────────────────────");
            if (failedSteps.Count > 0)
            {
                Log($"升级完成：{failedSteps.Count} 个步骤失败：{string.Join(", ", failedSteps)}");
                Log("升级提示：请检查失败步骤的错误信息并重新执行");
            }
            else
            {
                Log("升级完成：所有升级步骤执行完成");
                Log("升级提示：升级成功");
            }
            Log("────────────────────────────────────────");
        }

        /// <summary>
        /// 执行升级步骤（带重试）
        /// </summary>
        /// <param name="step"></param>
        /// <param name="action"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private async Task<UpgradeResult> ExecuteStepWithRetryAsync(StepConfig step, UpgradeAction action, int stepNumber)
        {
            var maxRetry = step.RetryCount;
            var retryDelay = step.RetryDelay;
            var attempt = 0;

            while (attempt <= maxRetry)
            {
                attempt++;

                try
                {
                    if (action.Option == UpgradeOption.Download)
                    {
                        return await ExecuteDownloadAsync(step, stepNumber);
                    }
                    return await Task.Run(() => action.Execute(step, stepNumber));
                }
                catch (Exception ex)
                {
                    LogStepInfo(stepNumber, "重试", $"第 {attempt} 次尝试失败：{ex.Message}");

                    if (attempt <= maxRetry)
                    {
                        LogStepInfo(stepNumber, "重试", $"等待 {retryDelay} 毫秒后重试...");
                        await Task.Delay(retryDelay);
                    }
                    else
                    {
                        return new UpgradeResult { Success = false, Message = $"经过 {maxRetry + 1} 次尝试后仍失败：{ex.Message}" };
                    }
                }
            }

            return new UpgradeResult { Success = false, Message = "执行失败" };
        }

        #region 升级操作
        /// <summary>
        /// 下载文件（异步）
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private async Task<UpgradeResult> ExecuteDownloadAsync(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Url))
            {
                return new UpgradeResult { Success = false, Message = "下载URL为空" };
            }

            var destPath = string.IsNullOrEmpty(step.File)
                ? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip")
                : step.File;

            try
            {
                LogStepInfo(stepNumber, "下载", $"从 {step.Url} 下载到 {destPath}");

                var response = await _HttpClient.GetAsync(step.Url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(destPath))
                {
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        if (totalBytes > 0)
                        {
                            var progress = (int)((totalRead * 100) / totalBytes);
                            LogStepProgress(stepNumber, progress, $"下载进度：{progress}%");
                        }
                    }
                }

                LogNewLine();
                step.File = destPath;
                return new UpgradeResult { Success = true, Message = $"文件下载完成，大小：{FormatFileSize(new FileInfo(destPath).Length)}" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"下载失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 下载文件（同步）
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteDownload(StepConfig step, int stepNumber)
        {
            return ExecuteDownloadAsync(step, stepNumber).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 上传文件（异步）
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private async Task<UpgradeResult> ExecuteUploadAsync(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Url))
            {
                return new UpgradeResult { Success = false, Message = "上传URL为空" };
            }

            if (string.IsNullOrWhiteSpace(step.File))
            {
                return new UpgradeResult { Success = false, Message = "本地文件路径为空" };
            }

            if (!File.Exists(step.File))
            {
                return new UpgradeResult { Success = false, Message = $"本地文件不存在：{step.File}" };
            }

            try
            {
                LogStepInfo(stepNumber, "上传", $"从 {step.File} 上传到 {step.Url}");

                using (var fileStream = File.OpenRead(step.File))
                using (var content = new StreamContent(fileStream))
                {
                    var fileName = Path.GetFileName(step.File);
                    content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                    {
                        Name = "\"file\"",
                        FileName = $"\"{fileName}\""
                    };
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                    var formData = new MultipartFormDataContent();
                    formData.Add(content, "file", fileName);

                    var response = await _HttpClient.PostAsync(step.Url, formData);
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    return new UpgradeResult { Success = true, Message = $"文件上传完成，响应：{responseContent.Substring(0, Math.Min(responseContent.Length, 100))}" };
                }
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"上传失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 上传文件（同步）
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteUpload(StepConfig step, int stepNumber)
        {
            return ExecuteUploadAsync(step, stepNumber).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteCommand(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Command))
            {
                return new UpgradeResult { Success = false, Message = "命令为空" };
            }

            try
            {
                LogStepInfo(stepNumber, "执行", $"命令：{step.Command} {step.Args ?? ""}");

                var parts = ParseCommand(step.Command);
                var exePath = parts.Item1;
                var exeArgs = parts.Item2;

                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"{exeArgs} {step.Args ?? string.Empty}".Trim(),
                    WorkingDirectory = string.IsNullOrEmpty(step.Path) ? AppContext.BaseDirectory : step.Path,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = System.Diagnostics.Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        var exited = process.WaitForExit(60000);

                        if (!string.IsNullOrEmpty(output))
                        {
                            LogStepInfo(stepNumber, "输出", output.Trim().Substring(0, Math.Min(200, output.Length)));
                        }
                        if (!string.IsNullOrEmpty(error))
                        {
                            LogStepInfo(stepNumber, "警告", error.Trim().Substring(0, Math.Min(200, error.Length)));
                        }

                        if (exited)
                        {
                            if (process.ExitCode == 0)
                            {
                                return new UpgradeResult { Success = true, Message = $"命令执行成功，退出码：{process.ExitCode}" };
                            }
                            else
                            {
                                return new UpgradeResult { Success = false, Message = $"命令执行失败，退出码：{process.ExitCode}" };
                            }
                        }
                        else
                        {
                            return new UpgradeResult { Success = true, Message = "命令进程在 60 秒内未退出，视为后台运行" };
                        }
                    }
                    else
                    {
                        return new UpgradeResult { Success = false, Message = "进程启动失败" };
                    }
                }
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"命令执行异常：{ex.Message}" };
            }
        }

        /// <summary>
        /// 启动应用
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteLaunch(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Command))
            {
                return new UpgradeResult { Success = false, Message = "命令为空" };
            }

            try
            {
                LogStepInfo(stepNumber, "启动", $"程序：{step.Command} {step.Args ?? ""}");

                var parts = ParseCommand(step.Command);
                var exePath = parts.Item1;
                var exeArgs = parts.Item2;

                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"{exeArgs} {step.Args ?? string.Empty}".Trim(),
                    WorkingDirectory = string.IsNullOrEmpty(step.Path) ? AppContext.BaseDirectory : step.Path,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                var process = System.Diagnostics.Process.Start(processStartInfo);
                if (process != null)
                {
                    return new UpgradeResult { Success = true, Message = $"程序启动成功，进程ID：{process.Id}" };
                }
                else
                {
                    return new UpgradeResult { Success = false, Message = "进程启动失败" };
                }
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"启动程序异常：{ex.Message}" };
            }
        }

        /// <summary>
        /// 压缩文件
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteZip(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Source))
            {
                return new UpgradeResult { Success = false, Message = "源路径为空" };
            }

            var destPath = string.IsNullOrEmpty(step.Destination)
                ? step.Source + ".zip"
                : step.Destination;

            try
            {
                if (Directory.Exists(step.Source))
                {
                    LogStepInfo(stepNumber, "压缩", $"目录：{step.Source} -> {destPath}");

                    var files = Directory.GetFiles(step.Source, "*", SearchOption.AllDirectories);
                    var totalFiles = files.Length;

                    Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? ".");

                    var startTime = DateTime.Now;

                    using (var zipStream = File.Create(destPath))
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            var filePath = files[i];

                            var progress = (int)((i + 1) * 100 / totalFiles);
                            LogStepProgress(stepNumber, progress, $"压缩中 [{i + 1}/{totalFiles}] {Path.GetFileName(filePath)}");

                            var relativePath = Path.GetRelativePath(step.Source, filePath);
                            archive.CreateEntryFromFile(filePath, relativePath, CompressionLevel.Optimal);
                        }
                    }

                    var elapsed = DateTime.Now - startTime;
                    LogNewLine();
                    return new UpgradeResult { Success = true, Message = $"目录压缩完成，共 {totalFiles} 个文件，大小：{FormatFileSize(new FileInfo(destPath).Length)}，耗时：{elapsed.TotalSeconds:F1}秒" };
                }
                else if (File.Exists(step.Source))
                {
                    LogStepInfo(stepNumber, "压缩", $"文件：{step.Source} -> {destPath}");

                    Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? ".");

                    using (var zipStream = File.Create(destPath))
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                    {
                        archive.CreateEntryFromFile(step.Source, Path.GetFileName(step.Source), CompressionLevel.Optimal);
                    }

                    return new UpgradeResult { Success = true, Message = $"文件压缩完成，大小：{FormatFileSize(new FileInfo(destPath).Length)}" };
                }
                else
                {
                    return new UpgradeResult { Success = false, Message = $"源路径不存在：{step.Source}" };
                }
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"压缩失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 解压文件
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteUnzip(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Source))
            {
                return new UpgradeResult { Success = false, Message = "源文件为空" };
            }

            if (!File.Exists(step.Source))
            {
                return new UpgradeResult { Success = false, Message = $"源文件不存在：{step.Source}" };
            }

            var destPath = string.IsNullOrEmpty(step.Destination) ? Path.GetDirectoryName(step.Source) ?? "." : step.Destination;

            try
            {
                LogStepInfo(stepNumber, "解压", $"{step.Source} -> {destPath}");

                var zipFileSize = new FileInfo(step.Source).Length;
                LogStepInfo(stepNumber, "统计", $"压缩包大小：{FormatFileSize(zipFileSize)}");

                if (zipFileSize > 100 * 1024 * 1024)
                {
                    LogStepInfo(stepNumber, "提示", $"压缩包较大（{FormatFileSize(zipFileSize)}），解压可能需要较长时间，请耐心等待...");
                }

                int totalEntries = 0;

                using (var archive = ZipFile.OpenRead(step.Source))
                {
                    var entries = archive.Entries.ToList();
                    totalEntries = entries.Count;

                    if (totalEntries == 0)
                    {
                        return new UpgradeResult { Success = false, Message = "压缩包为空" };
                    }

                    var totalUncompressedSize = entries.Sum(e => e.Length);
                    LogStepInfo(stepNumber, "统计", $"压缩包内共 {totalEntries} 个文件，解压后大小：{FormatFileSize(totalUncompressedSize)}");

                    if (totalEntries > 100)
                    {
                        LogStepInfo(stepNumber, "提示", $"文件数量较多（{totalEntries}个），解压可能需要较长时间，请耐心等待...");
                    }
                    if (totalUncompressedSize > 500 * 1024 * 1024)
                    {
                        LogStepInfo(stepNumber, "提示", $"解压后文件总大小较大（{FormatFileSize(totalUncompressedSize)}），解压可能需要较长时间，请耐心等待...");
                    }

                    Directory.CreateDirectory(destPath);

                    var startTime = DateTime.Now;

                    for (int i = 0; i < entries.Count; i++)
                    {
                        var entry = entries[i];
                        var path = Path.Combine(destPath, entry.FullName);

                        if (path.EndsWith("/"))
                        {
                            Directory.CreateDirectory(path);
                            continue;
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(path));

                        if (File.Exists(path) && !step.Overwrite)
                        {
                            LogStepInfo(stepNumber, "跳过", $"文件已存在且不覆盖：{entry.FullName}");
                            continue;
                        }

                        entry.ExtractToFile(path, step.Overwrite);

                        var progress = (int)((i + 1) * 100 / totalEntries);
                        LogStepProgress(stepNumber, progress, $"解压中 [{i + 1}/{totalEntries}] {entry.FullName}");
                    }

                    var elapsed = DateTime.Now - startTime;
                    LogNewLine();
                    return new UpgradeResult { Success = true, Message = $"解压完成，共 {totalEntries} 个文件，耗时：{elapsed.TotalSeconds:F1}秒" };
                }
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"解压失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 移动目录
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteMoveDir(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Source))
            {
                return new UpgradeResult { Success = false, Message = "源目录为空" };
            }

            if (string.IsNullOrWhiteSpace(step.Destination))
            {
                return new UpgradeResult { Success = false, Message = "目标目录为空" };
            }

            if (!Directory.Exists(step.Source))
            {
                return new UpgradeResult { Success = false, Message = $"源目录不存在：{step.Source}" };
            }

            try
            {
                LogStepInfo(stepNumber, "移动", $"目录：{step.Source} -> {step.Destination}");
                MoveDirectory(step.Source, step.Destination, step.Overwrite);
                Directory.Delete(step.Source, true);

                return new UpgradeResult { Success = true, Message = "目录移动完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"移动目录失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteMoveDoc(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Source))
            {
                return new UpgradeResult { Success = false, Message = "源文件为空" };
            }

            if (string.IsNullOrWhiteSpace(step.Destination))
            {
                return new UpgradeResult { Success = false, Message = "目标文件为空" };
            }

            if (!File.Exists(step.Source))
            {
                return new UpgradeResult { Success = false, Message = $"源文件不存在：{step.Source}" };
            }

            try
            {
                LogStepInfo(stepNumber, "移动", $"文件：{step.Source} -> {step.Destination}");

                Directory.CreateDirectory(Path.GetDirectoryName(step.Destination) ?? ".");

                if (File.Exists(step.Destination) && step.Overwrite)
                {
                    File.Delete(step.Destination);
                }

                File.Move(step.Source, step.Destination);

                return new UpgradeResult { Success = true, Message = "文件移动完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"移动文件失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 复制目录
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteCopyDir(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Source))
            {
                return new UpgradeResult { Success = false, Message = "源目录为空" };
            }

            if (string.IsNullOrWhiteSpace(step.Destination))
            {
                return new UpgradeResult { Success = false, Message = "目标目录为空" };
            }

            if (!Directory.Exists(step.Source))
            {
                return new UpgradeResult { Success = false, Message = $"源目录不存在：{step.Source}" };
            }

            try
            {
                LogStepInfo(stepNumber, "复制", $"目录：{step.Source} -> {step.Destination}");

                CopyDirectory(step.Source, step.Destination, step.Overwrite);

                return new UpgradeResult { Success = true, Message = "目录复制完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"复制目录失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteCopyDoc(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Source))
            {
                return new UpgradeResult { Success = false, Message = "源文件为空" };
            }

            if (string.IsNullOrWhiteSpace(step.Destination))
            {
                return new UpgradeResult { Success = false, Message = "目标文件为空" };
            }

            if (!File.Exists(step.Source))
            {
                return new UpgradeResult { Success = false, Message = $"源文件不存在：{step.Source}" };
            }

            try
            {
                LogStepInfo(stepNumber, "复制", $"文件：{step.Source} -> {step.Destination}");

                Directory.CreateDirectory(Path.GetDirectoryName(step.Destination) ?? ".");
                File.Copy(step.Source, step.Destination, step.Overwrite);

                return new UpgradeResult { Success = true, Message = "文件复制完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"复制文件失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteCreateDir(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Path))
            {
                return new UpgradeResult { Success = false, Message = "目录路径为空" };
            }

            try
            {
                LogStepInfo(stepNumber, "创建", $"目录：{step.Path}");

                Directory.CreateDirectory(step.Path);

                return new UpgradeResult { Success = true, Message = "目录创建完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"创建目录失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 创建文件
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteCreateDoc(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Path))
            {
                return new UpgradeResult { Success = false, Message = "文件路径为空" };
            }

            try
            {
                LogStepInfo(stepNumber, "创建", $"文件：{step.Path}");

                Directory.CreateDirectory(Path.GetDirectoryName(step.Path) ?? ".");

                if (File.Exists(step.Path) && !step.Overwrite)
                {
                    return new UpgradeResult { Success = false, Message = "文件已存在，且不允许覆盖" };
                }

                File.Create(step.Path).Close();

                return new UpgradeResult { Success = true, Message = "文件创建完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"创建文件失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 删除目录
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteDeleteDir(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Path))
            {
                return new UpgradeResult { Success = false, Message = "目录路径为空" };
            }

            if (!Directory.Exists(step.Path))
            {
                return new UpgradeResult { Success = true, Message = "目录不存在，无需删除" };
            }

            try
            {
                LogStepInfo(stepNumber, "删除", $"目录：{step.Path}");

                Directory.Delete(step.Path, true);

                return new UpgradeResult { Success = true, Message = "目录删除完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"删除目录失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteDeleteDoc(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.Path))
            {
                return new UpgradeResult { Success = false, Message = "文件路径为空" };
            }

            if (!File.Exists(step.Path))
            {
                return new UpgradeResult { Success = true, Message = "文件不存在，无需删除" };
            }

            try
            {
                LogStepInfo(stepNumber, "删除", $"文件：{step.Path}");

                File.Delete(step.Path);

                return new UpgradeResult { Success = true, Message = "文件删除完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"删除文件失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 更名目录
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteRenameDir(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.OldName))
            {
                return new UpgradeResult { Success = false, Message = "原目录名为空" };
            }

            if (string.IsNullOrWhiteSpace(step.NewName))
            {
                return new UpgradeResult { Success = false, Message = "新目录名为空" };
            }

            if (!Directory.Exists(step.OldName))
            {
                return new UpgradeResult { Success = false, Message = $"原目录不存在：{step.OldName}" };
            }

            try
            {
                LogStepInfo(stepNumber, "更名", $"目录：{step.OldName} -> {step.NewName}");
                if (Directory.Exists(step.NewName))
                {
                    if (!step.Overwrite)
                    {
                        return new UpgradeResult { Success = false, Message = "新目录已存在！" };
                    }

                    Directory.Delete(step.NewName, true);
                }

                Directory.Move(step.OldName, step.NewName);

                return new UpgradeResult { Success = true, Message = "目录更名完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"更名目录失败：{ex.Message}" };
            }
        }

        /// <summary>
        /// 更名文件
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepNumber"></param>
        /// <returns></returns>
        private UpgradeResult ExecuteRenameDoc(StepConfig step, int stepNumber)
        {
            if (string.IsNullOrWhiteSpace(step.OldName))
            {
                return new UpgradeResult { Success = false, Message = "原文件名为空" };
            }

            if (string.IsNullOrWhiteSpace(step.NewName))
            {
                return new UpgradeResult { Success = false, Message = "新文件名为空" };
            }

            if (!File.Exists(step.OldName))
            {
                return new UpgradeResult { Success = false, Message = $"原文件不存在：{step.OldName}" };
            }

            try
            {
                LogStepInfo(stepNumber, "更名", $"文件：{step.OldName} -> {step.NewName}");
                if (File.Exists(step.NewName))
                {
                    if (!step.Overwrite)
                    {
                        return new UpgradeResult { Success = false, Message = "新文件已存在！" };
                    }

                    File.Delete(step.NewName);
                }

                File.Move(step.OldName, step.NewName);

                return new UpgradeResult { Success = true, Message = "文件更名完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"更名文件失败：{ex.Message}" };
            }
        }
        #endregion

        #region 工具方法
        private UpgradeAction GetAction(UpgradeOption option)
        {
            _Actions.TryGetValue(option, out var action);
            return action;
        }

        /// <summary>
        /// 移动目录
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="destinationDir"></param>
        /// <param name="overwrite"></param>
        private void MoveDirectory(string sourceDir, string destinationDir, bool overwrite)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Move(file, destFile, overwrite);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                MoveDirectory(subDir, destSubDir, overwrite);
            }
        }

        /// <summary>
        /// 复制目录
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="destinationDir"></param>
        /// <param name="overwrite"></param>
        private void CopyDirectory(string sourceDir, string destinationDir, bool overwrite)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir, overwrite);
            }
        }

        /// <summary>
        /// 文件大小格式化
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private string FormatFileSize(long size)
        {
            var units = new string[] { "B", "KB", "MB", "GB", "TB", "PB" };
            int i = 0;
            while (size > 1024)
            {
                size = size >> 10;
                i++;
            }
            return size + units[i];
        }

        /// <summary>
        /// 命令行解析
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private Tuple<string, string> ParseCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return Tuple.Create(string.Empty, string.Empty);
            }

            command = command.Trim();

            if (command.StartsWith("\""))
            {
                var endQuote = command.IndexOf("\"", 1);
                if (endQuote > 0)
                {
                    var exe = command.Substring(1, endQuote - 1);
                    var args = command.Substring(endQuote + 1).Trim();
                    return Tuple.Create(exe, args);
                }
            }

            var spaceIndex = command.IndexOf(' ');
            if (spaceIndex > 0)
            {
                var exe = command.Substring(0, spaceIndex);
                var args = command.Substring(spaceIndex + 1);
                return Tuple.Create(exe, args);
            }

            return Tuple.Create(command, string.Empty);
        }
        #endregion

        #region 日志输出
        private void Log(string message)
        {
            LogToFile(message);
            _View?.Log(message);
        }

        private void LogNewLine()
        {
            _View?.LogNewLine();
        }

        private void LogStep(int step, int count, string message)
        {
            LogToFile($"step:{step}, count:{count}, message:{message}");
            _View?.LogStep(step, count, message);
        }

        private void LogStepInfo(int step, string info, string message)
        {
            LogToFile($"step:{step}, info:{info}, message:{message}");
            _View?.LogStepInfo(step, info, message);
        }

        private void LogStepWait(int step, int time, string message)
        {
            LogToFile($"step:{step}, time:{time}, message:{message}");
            _View?.LogStepWait(step, time, message);
        }

        private void LogStepStatus(int step, StepStatus status, string message)
        {
            LogToFile($"step:{step}, status:{status}, message:{message}");
            _View?.LogStepStatus(step, status, message);
        }

        private void LogStepProgress(int step, int progress, string message)
        {
            LogToFile($"step:{step}, progress:{progress}, message:{message}");
            _View?.LogStepProgress(step, progress, message);
        }

        /// <summary>
        /// 记录日志到文件
        /// </summary>
        /// <param name="message"></param>
        private void LogToFile(string message)
        {
            if (!_Config.LogToFile)
            {
                return;
            }

            var now = DateTime.Now;

            if (_writer == null)
            {
                var date = now.ToString("yyyyMMdd");
                _writer = new StreamWriter($"Upgrade_{date}.log") { AutoFlush = true };
            }

            var time = now.ToString("yyyy-MM-dd HH:mm:ss");
            //await _writer.WriteLineAsync($"{time} {message}");
            _writer.WriteLine($"{time} {message}");
        }
        #endregion

        /// <summary>
        /// 析构
        /// </summary>
        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Close();
                _writer.Dispose();
                _writer = null;
            }
        }
    }
}
