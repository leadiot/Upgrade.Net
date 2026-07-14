using Com.Scm.Upgrade.Config;
using System.IO.Compression;

namespace Com.Scm.Upgrade
{
    public class Upgrade
    {
        public const int MAJOR = 1;
        public const int MINOR = 1;
        public const int PATCH = 3;
        public const int BUILD = 3;
        public const string RELEASE = "2026-07-14";

        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        private readonly Dictionary<UpgradeOption, UpgradeAction> _actions = new Dictionary<UpgradeOption, UpgradeAction>();

        public Upgrade()
        {
            InitializeActions();
        }

        private void InitializeActions()
        {
            _actions[UpgradeOption.Download] = new UpgradeAction
            {
                Option = UpgradeOption.Download,
                Title = "下载文件",
                Description = "从指定URL下载文件",
                Execute = ExecuteDownload
            };

            _actions[UpgradeOption.Command] = new UpgradeAction
            {
                Option = UpgradeOption.Command,
                Title = "执行命令",
                Description = "执行指定的系统命令",
                Execute = ExecuteCommand
            };

            _actions[UpgradeOption.Zip] = new UpgradeAction
            {
                Option = UpgradeOption.Zip,
                Title = "压缩文件",
                Description = "将指定目录或文件压缩为ZIP",
                Execute = ExecuteZip
            };

            _actions[UpgradeOption.Unzip] = new UpgradeAction
            {
                Option = UpgradeOption.Unzip,
                Title = "解压文件",
                Description = "将ZIP文件解压到指定目录",
                Execute = ExecuteUnzip
            };

            _actions[UpgradeOption.MoveDir] = new UpgradeAction
            {
                Option = UpgradeOption.MoveDir,
                Title = "移动目录",
                Description = "将目录移动到指定位置",
                Execute = ExecuteMoveDir
            };

            _actions[UpgradeOption.MoveDoc] = new UpgradeAction
            {
                Option = UpgradeOption.MoveDoc,
                Title = "移动文件",
                Description = "将文件移动到指定位置",
                Execute = ExecuteMoveDoc
            };

            _actions[UpgradeOption.CopyDir] = new UpgradeAction
            {
                Option = UpgradeOption.CopyDir,
                Title = "复制目录",
                Description = "将目录复制到指定位置",
                Execute = ExecuteCopyDir
            };

            _actions[UpgradeOption.CopyDoc] = new UpgradeAction
            {
                Option = UpgradeOption.CopyDoc,
                Title = "复制文件",
                Description = "将文件复制到指定位置",
                Execute = ExecuteCopyDoc
            };

            _actions[UpgradeOption.CreateDir] = new UpgradeAction
            {
                Option = UpgradeOption.CreateDir,
                Title = "创建目录",
                Description = "创建指定目录",
                Execute = ExecuteCreateDir
            };

            _actions[UpgradeOption.CreateDoc] = new UpgradeAction
            {
                Option = UpgradeOption.CreateDoc,
                Title = "创建文件",
                Description = "创建指定文件",
                Execute = ExecuteCreateDoc
            };

            _actions[UpgradeOption.DeleteDir] = new UpgradeAction
            {
                Option = UpgradeOption.DeleteDir,
                Title = "删除目录",
                Description = "删除指定目录",
                Execute = ExecuteDeleteDir
            };

            _actions[UpgradeOption.DeleteDoc] = new UpgradeAction
            {
                Option = UpgradeOption.DeleteDoc,
                Title = "删除文件",
                Description = "删除指定文件",
                Execute = ExecuteDeleteDoc
            };

            _actions[UpgradeOption.RenameDir] = new UpgradeAction
            {
                Option = UpgradeOption.RenameDir,
                Title = "重命名目录",
                Description = "重命名指定目录",
                Execute = ExecuteRenameDir
            };

            _actions[UpgradeOption.RenameDoc] = new UpgradeAction
            {
                Option = UpgradeOption.RenameDoc,
                Title = "重命名文件",
                Description = "重命名指定文件",
                Execute = ExecuteRenameDoc
            };
        }

        public void Start()
        {
            Log("═══════════════════════════════════════════════");
            Log($"            Upgrade.Net v{MAJOR}.{MINOR}.{PATCH}.{BUILD}");
            Log("═══════════════════════════════════════════════");
            Log("");

            try
            {
                var config = UpgradeConfig.Load();
                if (config == null)
                {
                    Log("[错误] 配置文件 upgrade.json 不存在，结束升级任务");
                    ExitApplication();
                    return;
                }

                if (!string.IsNullOrEmpty(config.Title))
                {
                    Log($"[信息] 应用名称：{config.Title}");
                }

                if (!string.IsNullOrEmpty(config.OldVersion) && !string.IsNullOrEmpty(config.NewVersion))
                {
                    Log($"[信息] 版本升级：{config.OldVersion} -> {config.NewVersion}");
                }

                if (!string.IsNullOrEmpty(config.VerInfo))
                {
                    Log("[信息] 更新说明：");
                    Log(config.VerInfo);
                }

                Log("");

                if (config.Steps != null && config.Steps.Count > 0)
                {
                    ExecuteSteps(config);
                }
                else
                {
                    var legacySteps = BuildLegacySteps(config);
                    if (legacySteps.Count > 0)
                    {
                        config.Steps = legacySteps;
                        Log("[信息] 使用传统配置模式，自动构建升级步骤");
                        ExecuteSteps(config);
                    }
                    else
                    {
                        Log("[警告] 未配置升级步骤，请在 upgrade.json 中配置 Steps");
                    }
                }

                Log("");
                Log("═══════════════════════════════════════════════");
                Log("            升级任务完成");
                Log("═══════════════════════════════════════════════");

                CloseApplication(config.AutoClose);
            }
            catch (Exception ex)
            {
                Log($"");
                Log($"[错误] 更新失败：{ex.Message}");
                Log(ex.StackTrace);
                ExitApplication();
            }
        }

        private void ExecuteSteps(UpgradeConfig config)
        {
            Log($"[步骤] 开始执行 {config.Steps.Count} 个升级步骤...");
            Log("");

            var failedSteps = new List<int>();

            for (int i = 0; i < config.Steps.Count; i++)
            {
                var step = config.Steps[i];
                var stepNumber = i + 1;

                var action = GetAction(step.Option);
                if (action == null)
                {
                    Log($"[步骤{stepNumber}/{config.Steps.Count}] [跳过] 未知操作类型：{step.Option}");
                    continue;
                }

                var title = string.IsNullOrEmpty(step.Title) ? action.Title : step.Title;
                var description = string.IsNullOrEmpty(step.Description) ? action.Description : step.Description;

                Log($"[步骤{stepNumber}/{config.Steps.Count}] {title}");
                Log($"   {description}");

                var result = ExecuteStepWithRetry(step, action, stepNumber);

                if (!result.Success)
                {
                    failedSteps.Add(stepNumber);

                    if (!step.ContinueOnError)
                    {
                        Log($"   [终止] 步骤失败且不允许继续，终止升级流程");
                        throw new Exception($"步骤 {stepNumber} 执行失败：{result.Message}");
                    }

                    Log($"   [继续] 步骤失败但允许继续，继续执行后续步骤");
                }
                else
                {
                    Log($"   [完成] {result.Message}");

                    if (step.WaitTime > 0)
                    {
                        Log($"   [等待] 等待 {step.WaitTime} 秒...");
                        Thread.Sleep(step.WaitTime * 1000);
                    }
                }

                Log("");
            }

            if (failedSteps.Count > 0)
            {
                Log($"[步骤] 升级完成，{failedSteps.Count} 个步骤失败：{string.Join(", ", failedSteps)}");
            }
            else
            {
                Log("[步骤] 所有升级步骤执行完成");
            }
        }

        private UpgradeResult ExecuteStepWithRetry(StepConfig step, UpgradeAction action, int stepNumber)
        {
            var maxRetry = step.RetryCount;
            var retryDelay = step.RetryDelay;
            var attempt = 0;

            while (attempt <= maxRetry)
            {
                attempt++;

                try
                {
                    return action.Execute(step);
                }
                catch (Exception ex)
                {
                    Log($"   [重试] 第 {attempt} 次尝试失败：{ex.Message}");

                    if (attempt <= maxRetry)
                    {
                        Log($"   [重试] 等待 {retryDelay} 毫秒后重试...");
                        Thread.Sleep(retryDelay);
                    }
                    else
                    {
                        return new UpgradeResult { Success = false, Message = $"经过 {maxRetry + 1} 次尝试后仍失败：{ex.Message}" };
                    }
                }
            }

            return new UpgradeResult { Success = false, Message = "执行失败" };
        }

        private UpgradeAction GetAction(UpgradeOption option)
        {
            _actions.TryGetValue(option, out var action);
            return action;
        }

        private List<StepConfig> BuildLegacySteps(UpgradeConfig config)
        {
            var steps = new List<StepConfig>();

            if (!string.IsNullOrEmpty(config.Url))
            {
                var installPath = string.IsNullOrEmpty(config.InstallPath) 
                    ? AppDomain.CurrentDomain.BaseDirectory 
                    : config.InstallPath;

                var downloadFile = Path.Combine(installPath, "update.zip");

                steps.Add(new StepConfig
                {
                    Option = UpgradeOption.Download,
                    Title = "下载更新包",
                    Description = "从远程服务器下载更新包",
                    WaitTime = 0,
                    Url = config.Url,
                    File = downloadFile
                });

                if ((config.AutoBackup || config.Backup != null) && 
                    (!string.IsNullOrEmpty(config.BackupPath) || (config.Backup != null && !string.IsNullOrEmpty(config.Backup.Path))))
                {
                    var backupPath = !string.IsNullOrEmpty(config.BackupPath) 
                        ? config.BackupPath 
                        : (config.Backup?.Path ?? installPath);

                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var backupFile = Path.Combine(backupPath, $"backup_{timestamp}.zip");

                    steps.Add(new StepConfig
                    {
                        Option = UpgradeOption.Zip,
                        Title = "备份现有文件",
                        Description = "备份当前版本文件",
                        WaitTime = 0,
                        Source = installPath,
                        Destination = backupFile
                    });
                }

                steps.Add(new StepConfig
                {
                    Option = UpgradeOption.Unzip,
                    Title = "解压更新包",
                    Description = "将更新包解压到安装目录",
                    WaitTime = 0,
                    Source = downloadFile,
                    Destination = installPath,
                    Overwrite = true
                });

                if (config.Launch != null && !string.IsNullOrEmpty(config.Launch.Command))
                {
                    steps.Add(new StepConfig
                    {
                        Option = UpgradeOption.Command,
                        Title = "启动应用程序",
                        Description = "启动升级后的应用程序",
                        WaitTime = 0,
                        Command = config.Launch.Command,
                        Args = config.Launch.Args,
                        Path = installPath
                    });
                }

                steps.Add(new StepConfig
                {
                    Option = UpgradeOption.DeleteDoc,
                    Title = "清理临时文件",
                    Description = "删除下载的更新包",
                    WaitTime = 0,
                    Path = downloadFile,
                    ContinueOnError = true
                });
            }
            else if (config.Offline != null && !string.IsNullOrEmpty(config.Offline.File))
            {
                var installPath = string.IsNullOrEmpty(config.InstallPath)
                    ? AppDomain.CurrentDomain.BaseDirectory
                    : config.InstallPath;

                var offlineFile = config.Offline.File;
                var destFile = Path.Combine(installPath, Path.GetFileName(offlineFile));

                steps.Add(new StepConfig
                {
                    Option = UpgradeOption.CopyDoc,
                    Title = "复制离线升级文件",
                    Description = "将离线升级文件复制到安装目录",
                    WaitTime = 0,
                    Source = offlineFile,
                    Destination = destFile,
                    Overwrite = true
                });

                if (config.Offline.Time > 0)
                {
                    for (int i = config.Offline.Time; i > 0; i--)
                    {
                        Log($"   [等待] 倒计时：{i} 秒");
                        Thread.Sleep(1000);
                    }
                }

                if ((config.AutoBackup || config.Backup != null) &&
                    (!string.IsNullOrEmpty(config.BackupPath) || (config.Backup != null && !string.IsNullOrEmpty(config.Backup.Path))))
                {
                    var backupPath = !string.IsNullOrEmpty(config.BackupPath)
                        ? config.BackupPath
                        : (config.Backup?.Path ?? installPath);

                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var backupFile = Path.Combine(backupPath, $"backup_{timestamp}.zip");

                    steps.Add(new StepConfig
                    {
                        Option = UpgradeOption.Zip,
                        Title = "备份现有文件",
                        Description = "备份当前版本文件",
                        WaitTime = 0,
                        Source = installPath,
                        Destination = backupFile
                    });
                }

                steps.Add(new StepConfig
                {
                    Option = UpgradeOption.Unzip,
                    Title = "解压更新包",
                    Description = "将更新包解压到安装目录",
                    WaitTime = 0,
                    Source = destFile,
                    Destination = installPath,
                    Overwrite = true
                });

                if (config.Launch != null && !string.IsNullOrEmpty(config.Launch.Command))
                {
                    steps.Add(new StepConfig
                    {
                        Option = UpgradeOption.Command,
                        Title = "启动应用程序",
                        Description = "启动升级后的应用程序",
                        WaitTime = 0,
                        Command = config.Launch.Command,
                        Args = config.Launch.Args,
                        Path = installPath
                    });
                }
            }

            return steps;
        }

        private UpgradeResult ExecuteDownload(StepConfig step)
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
                Log($"   [下载] 从 {step.Url} 下载到 {destPath}");

                var response = _httpClient.GetAsync(step.Url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                using (var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                using (var fileStream = File.Create(destPath))
                {
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int bytesRead;
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        if (totalBytes > 0)
                        {
                            var progress = (int)((totalRead * 100) / totalBytes);
                            Console.Write($"\r   [下载] 进度：{progress}% ({FormatFileSize(totalRead)} / {FormatFileSize(totalBytes)})");
                        }
                        else
                        {
                            Console.Write($"\r   [下载] 进度：{FormatFileSize(totalRead)}");
                        }
                    }
                    Console.WriteLine();
                }

                step.File = destPath;
                return new UpgradeResult { Success = true, Message = $"文件下载完成，大小：{FormatFileSize(new FileInfo(destPath).Length)}" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"下载失败：{ex.Message}" };
            }
        }

        private UpgradeResult ExecuteCommand(StepConfig step)
        {
            if (string.IsNullOrWhiteSpace(step.Command))
            {
                return new UpgradeResult { Success = false, Message = "命令为空" };
            }

            try
            {
                Log($"   [执行] 命令：{step.Command} {step.Args ?? ""}");

                var parts = ParseCommand(step.Command);
                var exePath = parts.Item1;
                var exeArgs = parts.Item2;

                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"{exeArgs} {step.Args ?? string.Empty}".Trim(),
                    WorkingDirectory = string.IsNullOrEmpty(step.Path) ? AppDomain.CurrentDomain.BaseDirectory : step.Path,
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
                            Log($"   [输出] {output.Trim().Substring(0, Math.Min(200, output.Length))}");
                        }
                        if (!string.IsNullOrEmpty(error))
                        {
                            Log($"   [警告] {error.Trim().Substring(0, Math.Min(200, error.Length))}");
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

        private UpgradeResult ExecuteZip(StepConfig step)
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
                    Log($"   [压缩] 目录：{step.Source} -> {destPath}");

                    var files = Directory.GetFiles(step.Source, "*", SearchOption.AllDirectories);
                    var totalFiles = files.Length;

                    Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? ".");

                    using (var zipStream = File.Create(destPath))
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            var filePath = files[i];
                            var relativePath = Path.GetRelativePath(step.Source, filePath);
                            archive.CreateEntryFromFile(filePath, relativePath, CompressionLevel.Optimal);

                            var progress = (int)((i + 1) * 100 / totalFiles);
                            Console.Write($"\r   [压缩] 进度：{i + 1}/{totalFiles} ({progress}%)");
                        }
                    }
                    Console.WriteLine();

                    return new UpgradeResult { Success = true, Message = $"目录压缩完成，共 {totalFiles} 个文件，大小：{FormatFileSize(new FileInfo(destPath).Length)}" };
                }
                else if (File.Exists(step.Source))
                {
                    Log($"   [压缩] 文件：{step.Source} -> {destPath}");

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

        private UpgradeResult ExecuteUnzip(StepConfig step)
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
                Log($"   [解压] {step.Source} -> {destPath}");
                int totalEntries = 0;

                using (var archive = ZipFile.OpenRead(step.Source))
                {
                    var entries = archive.Entries.ToList();
                    totalEntries = entries.Count;

                    if (totalEntries == 0)
                    {
                        return new UpgradeResult { Success = false, Message = "压缩包为空" };
                    }

                    Directory.CreateDirectory(destPath);

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
                            continue;
                        }

                        entry.ExtractToFile(path, step.Overwrite);

                        var progress = (int)((i + 1) * 100 / totalEntries);
                        Console.Write($"\r   [解压] 进度：{i + 1}/{totalEntries} ({progress}%)");
                    }
                    Console.WriteLine();
                }

                return new UpgradeResult { Success = true, Message = $"解压完成，共 {totalEntries} 个文件" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"解压失败：{ex.Message}" };
            }
        }

        private UpgradeResult ExecuteMoveDir(StepConfig step)
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
                Log($"   [移动] 目录：{step.Source} -> {step.Destination}");

                if (Directory.Exists(step.Destination) && step.Overwrite)
                {
                    Directory.Delete(step.Destination, true);
                }

                Directory.Move(step.Source, step.Destination);

                return new UpgradeResult { Success = true, Message = "目录移动完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"移动目录失败：{ex.Message}" };
            }
        }

        private UpgradeResult ExecuteMoveDoc(StepConfig step)
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
                Log($"   [移动] 文件：{step.Source} -> {step.Destination}");

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

        private UpgradeResult ExecuteCopyDir(StepConfig step)
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
                Log($"   [复制] 目录：{step.Source} -> {step.Destination}");

                CopyDirectory(step.Source, step.Destination, step.Overwrite);

                return new UpgradeResult { Success = true, Message = "目录复制完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"复制目录失败：{ex.Message}" };
            }
        }

        private UpgradeResult ExecuteCopyDoc(StepConfig step)
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
                Log($"   [复制] 文件：{step.Source} -> {step.Destination}");

                Directory.CreateDirectory(Path.GetDirectoryName(step.Destination) ?? ".");
                File.Copy(step.Source, step.Destination, step.Overwrite);

                return new UpgradeResult { Success = true, Message = "文件复制完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"复制文件失败：{ex.Message}" };
            }
        }

        private UpgradeResult ExecuteCreateDir(StepConfig step)
        {
            if (string.IsNullOrWhiteSpace(step.Path))
            {
                return new UpgradeResult { Success = false, Message = "目录路径为空" };
            }

            try
            {
                Log($"   [创建] 目录：{step.Path}");

                Directory.CreateDirectory(step.Path);

                return new UpgradeResult { Success = true, Message = "目录创建完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"创建目录失败：{ex.Message}" };
            }
        }

        private UpgradeResult ExecuteCreateDoc(StepConfig step)
        {
            if (string.IsNullOrWhiteSpace(step.Path))
            {
                return new UpgradeResult { Success = false, Message = "文件路径为空" };
            }

            try
            {
                Log($"   [创建] 文件：{step.Path}");

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

        private UpgradeResult ExecuteDeleteDir(StepConfig step)
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
                Log($"   [删除] 目录：{step.Path}");

                Directory.Delete(step.Path, true);

                return new UpgradeResult { Success = true, Message = "目录删除完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"删除目录失败：{ex.Message}" };
            }
        }

        private UpgradeResult ExecuteDeleteDoc(StepConfig step)
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
                Log($"   [删除] 文件：{step.Path}");

                File.Delete(step.Path);

                return new UpgradeResult { Success = true, Message = "文件删除完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"删除文件失败：{ex.Message}" };
            }
        }

        private UpgradeResult ExecuteRenameDir(StepConfig step)
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
                Log($"   [重命名] 目录：{step.OldName} -> {step.NewName}");
                if (Directory.Exists(step.NewName))
                {
                    if (!step.Overwrite)
                    {
                        return new UpgradeResult { Success = false, Message = "新目录已存在！" };
                    }

                    Directory.Delete(step.NewName, true);
                }

                Directory.Move(step.OldName, step.NewName);

                return new UpgradeResult { Success = true, Message = "目录重命名完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"重命名目录失败：{ex.Message}" };
            }
        }

        private UpgradeResult ExecuteRenameDoc(StepConfig step)
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
                Log($"   [重命名] 文件：{step.OldName} -> {step.NewName}");
                if (File.Exists(step.NewName))
                {
                    if (!step.Overwrite)
                    {
                        return new UpgradeResult { Success = false, Message = "新文件已存在！" };
                    }

                    File.Delete(step.NewName);
                }

                File.Move(step.OldName, step.NewName);

                return new UpgradeResult { Success = true, Message = "文件重命名完成" };
            }
            catch (Exception ex)
            {
                return new UpgradeResult { Success = false, Message = $"重命名文件失败：{ex.Message}" };
            }
        }

        private void CopyDirectory(string sourceDir, string destDir, bool overwrite)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir, overwrite);
            }
        }

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

        private void CloseApplication(bool autoClose)
        {
            if (autoClose)
            {
                Log("");
                Log("   [信息] 升级程序即将退出...");
                Thread.Sleep(3000);
                Environment.Exit(0);
                return;
            }

            ExitApplication();
        }

        private void ExitApplication()
        {
            Log("");
            Log("[信息] 升级程序已结束，按任意键退出...");
            Console.ReadKey();
        }

        private void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
