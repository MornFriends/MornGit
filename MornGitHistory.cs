using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MornGit
{
    public class MornGitHistory
    {
        private readonly MornGitProcess _process;
        private readonly string _url;
        private readonly Action _toRefreshStatus;
        private readonly List<string> _newCommits = new();
        private readonly List<string> _existCommits = new();

        public MornGitHistory(MornGitProcess process, string url, Action toRefreshStatus)
        {
            _process = process;
            _url = url;
            _toRefreshStatus = toRefreshStatus;
        }

        private bool _isRefreshing;

        public async UniTask RefreshAsync()
        {
            if (_isRefreshing)
            {
                return;
            }

            _isRefreshing = true;
            _newCommits.Clear();
            _existCommits.Clear();
            var currentBranch = await _process.CurrentBranchAsync();
            var currentHash = (await _process.Async($"rev-parse \"{currentBranch}\""))[..7];
            var originHash = (await _process.Async($"rev-parse origin/\"{currentBranch}\""))[..7];
            var result = await _process.Async("log --oneline --first-parent -5");
            var histories = result.Split('\n').ToList();
            var addFlag = false;
            foreach (var commitName in histories)
            {
                if (commitName.Contains(currentHash))
                {
                    addFlag = true;
                }

                if (commitName.Contains(originHash))
                {
                    addFlag = false;
                }

                if (addFlag)
                {
                    _newCommits.Add(commitName);
                }
                else
                {
                    _existCommits.Add(commitName);
                }
            }

            _isRefreshing = false;
        }

        private readonly List<UniTask> _drawTaskList = new();

        public async UniTask DrawHistoryAsync()
        {
            if (_drawTaskList.Count > 0)
            {
                return;
            }

            GUILayout.Label("ツリー");
            MornGitUtil.Indent(() =>
            {
                MornGitUtil.H(() =>
                {
                    if (GUILayout.Button("Pull", GUILayout.Height(40)))
                    {
                        _drawTaskList.Add(_process.Async("pull --prune"));
                    }

                    var canPush = _newCommits.Count > 0;
                    MornGitUtil.EnableColor(canPush, Color.green, () =>
                    {
                        if (GUILayout.Button("Push", GUILayout.Height(40)))
                        {
                            _drawTaskList.Add(_process.Async("push"));
                        }
                    });
                });
                MornGitUtil.Box(() =>
                {
                    foreach (var history in _newCommits)
                    {
                        UndoCommit(history == _newCommits[0], history);
                    }

                    foreach (var history in _existCommits)
                    {
                        OpenCommit(history);
                    }
                });
            });
            if (_drawTaskList.Count > 0)
            {
                foreach (var task in _drawTaskList)
                {
                    await task;
                }

                _toRefreshStatus();
                await RefreshAsync();
                _drawTaskList.Clear();
            }
        }

        private void UndoCommit(bool canUndo, string commit)
        {
            using (new GUILayout.HorizontalScope())
            {
                MornGitUtil.Enable(canUndo, () =>
                {
                    if (GUILayout.Button("Reset", GUILayout.Width(50)))
                    {
                        _drawTaskList.Add(ResetAsync(commit));
                    }
                });
                MornGitUtil.Color(Color.green, () =>
                {
                    GUILayout.Label(commit);
                });
            }
        }

        private async UniTask ResetAsync(string commit)
        {
            var commitHash = commit[..7];
            var fullHash = await _process.Async($"rev-parse {commitHash}");
            await _process.Async($"reset --mixed {fullHash}~1");
        }

        private void OpenCommit(string commit)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open", GUILayout.Width(50)))
                {
                    _drawTaskList.Add(OpenAsync(commit));
                }

                GUILayout.Label(commit);
            }
        }

        private async UniTask OpenAsync(string commit)
        {
            var commitHash = commit[..7];
            var fullHash = await _process.Async($"rev-parse {commitHash}");

            // 末尾の.gitがあれば消す
            var newUrl = _url.EndsWith(".git") ? _url[..^4] : _url;
            Application.OpenURL($"{newUrl}/commit/{fullHash}");
        }
    }
}