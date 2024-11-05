using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MornGit
{
    public class MornGitBranch
    {
        private readonly MornGitProcess _process;
        private readonly Action _checkouted;
        private readonly List<string> _localBranchies = new();
        private readonly List<string> _remoteBranchies = new();
        private readonly List<string> _localRemoteBranchies = new();
        public string CurrentBranch { get; private set; }
        private string _newBranch;

        public MornGitBranch(MornGitProcess process, Action checkouted)
        {
            _process = process;
            _checkouted = checkouted;
        }

        private bool _isRefreshing;

        public async UniTask RefreshAsync()
        {
            if (_isRefreshing)
            {
                return;
            }

            _isRefreshing = true;
            _localBranchies.Clear();
            _remoteBranchies.Clear();
            _localRemoteBranchies.Clear();
            CurrentBranch = await _process.CurrentBranchAsync();
            var result = await _process.Async("branch -a");
            var branches = result.Split('\n').Select(x => x[2..]);
            foreach (var branch in branches)
            {
                if (branch.StartsWith("remotes/origin/HEAD"))
                {
                    continue;
                }

                if (branch.StartsWith("remotes/origin/"))
                {
                    _remoteBranchies.Add(branch.Substring(15));
                }
                else
                {
                    _localBranchies.Add(branch.Trim());
                }
            }

            for (var i = _localBranchies.Count - 1; i >= 0; i--)
            {
                var branch = _localBranchies[i];
                if (_remoteBranchies.Contains(branch))
                {
                    _localBranchies.Remove(branch);
                    _remoteBranchies.Remove(branch);
                    _localRemoteBranchies.Add(branch);
                }
            }

            _isRefreshing = false;
        }

        private readonly List<UniTask> _drawTaskList = new();

        public async UniTask DrawBranchAsync()
        {
            if (_drawTaskList.Count > 0)
            {
                return;
            }

            GUILayout.Label($"ブランチ：{CurrentBranch}", GUILayout.ExpandWidth(true));
            MornGitUtil.Indent(() =>
            {
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Fetch", GUILayout.Width(100), GUILayout.ExpandHeight(true)))
                    {
                        _drawTaskList.Add(_process.Async("fetch --prune"));
                    }

                    using (new GUILayout.VerticalScope())
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            var hasValidName = !string.IsNullOrEmpty(_newBranch) &&
                                               !_localBranchies.Contains(_newBranch) &&
                                               !_remoteBranchies.Contains(_newBranch) &&
                                               !_localRemoteBranchies.Contains(_newBranch);
                            MornGitUtil.Enable(hasValidName, () =>
                            {
                                if (GUILayout.Button("New Branch", GUILayout.Width(100)))
                                {
                                    _drawTaskList.Add(_process.Async($"checkout -b \"{_newBranch}\""));
                                }
                            });
                            _newBranch = GUILayout.TextField(_newBranch);
                        }

                        foreach (var branch in _localRemoteBranchies)
                        {
                            BranchRow("Local & Remote: ", branch, false);
                        }

                        foreach (var branch in _localBranchies)
                        {
                            BranchRow("Local: ", branch, true);
                        }

                        foreach (var branch in _remoteBranchies)
                        {
                            BranchRow("Remote: ", branch, false);
                        }
                    }
                }
            });
            if (_drawTaskList.Count > 0)
            {
                foreach (var task in _drawTaskList)
                {
                    await task;
                }

                await RefreshAsync();
                _checkouted?.Invoke();
                _drawTaskList.Clear();
            }
        }

        private void BranchRow(string prefix, string branch, bool canDelete)
        {
            using (new GUILayout.HorizontalScope())
            {
                MornGitUtil.Enable(branch != CurrentBranch, () =>
                {
                    if (GUILayout.Button("Checkout", GUILayout.Width(70)))
                    {
                        _drawTaskList.Add(_process.Async($"fetch origin {branch}"));
                        _drawTaskList.Add(_process.Async($"checkout \"{branch}\""));
                    }
                });
                MornGitUtil.ColorIf(branch == CurrentBranch, Color.green, () =>
                {
                    GUILayout.Label($"{prefix}{branch}");
                });
                if (canDelete)
                {
                    MornGitUtil.Enable(branch != CurrentBranch, () =>
                    {
                        if (GUILayout.Button("Delete", GUILayout.Width(70)))
                        {
                            _drawTaskList.Add(_process.Async($"branch -D \"{branch}\""));
                        }
                    });
                }
            }
        }
    }
}