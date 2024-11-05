using System.Diagnostics;
using System.Text;
using Cysharp.Threading.Tasks;

namespace MornGit
{
    public class MornGitProcess
    {
        public string Path { get; private set; }
        private readonly Process _process;

        public MornGitProcess(string path)
        {
            Path = path;
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = path,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };
        }

        public async UniTask<string> Async(string command)
        {
            _process.StartInfo.Arguments = command;
            _process.Start();
            await UniTask.WaitUntil(() => _process.HasExited);
            var output = await _process.StandardOutput.ReadToEndAsync();
            var result = output.TrimEnd('\n');
            MornGitUtil.Log($"Command: git {command}\nResult: {result}");
            return result;
        }

        public async UniTask<string> CurrentBranchAsync()
        {
            return await Async("branch --show-current");
        }
    }
}