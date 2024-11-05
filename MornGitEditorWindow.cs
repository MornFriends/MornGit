using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace MornGit
{
    public class MornGitEditorWindow : EditorWindow
    {
        private MornGitRepository _repository;
        private readonly List<MornGitRepository> _submodules = new();
        private Vector2 _scrollPosition;

        [MenuItem("Tools/MornGit")]
        private static void Open()
        {
            GetWindow<MornGitEditorWindow>("MornGit");
        }

        private async void OnEnable()
        {
            var path = Path.GetDirectoryName(Application.dataPath);
            var process = new MornGitProcess(path);
            _repository = await GenerateRepository(process);
            _submodules.Clear();
            var result = await process.Async("submodule status");
            foreach (var line in result.Split('\n'))
            {
                var submoduleRelativePath = line.Trim().Split(' ')[1];
                var submodulePath = Path.Combine(path, submoduleRelativePath);
                var subModuleProcess = new MornGitProcess(submodulePath);
                _submodules.Add(await GenerateRepository(subModuleProcess));
            }
        }

        private async UniTask<MornGitRepository> GenerateRepository(MornGitProcess process)
        {
            var url = await process.Async("remote get-url origin");
            var repositoryName = url.Split('/').Last().Split('.').First();
            var repository = new MornGitRepository(process, url, repositoryName);
            await repository.RefreshAsync();
            return repository;
        }

        private void OnGUI()
        {
            using (var scroll = new GUILayout.ScrollViewScope(_scrollPosition))
            {
                if (_repository != null)
                {
                    _repository.OnGUI();
                }

                foreach (var submodule in _submodules)
                {
                    submodule.OnGUI();
                }

                _scrollPosition = scroll.scrollPosition;
            }
        }
    }
}