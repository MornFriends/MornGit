using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace MornGit
{
    public class MornGitStatus
    {
        private readonly MornGitProcess _process;
        private string _commitMessage;
        private readonly Action _commited;
        // TODO NewとModified混合でソート
        // Deleted Renamed
        private readonly List<string> _stagedFiles = new();
        private readonly List<string> _unstagedFiles = new();
        private readonly List<string> _untrackedFiles = new();
        public int ChangeCount => _stagedFiles.Count + _unstagedFiles.Count + _untrackedFiles.Count;

        public MornGitStatus(MornGitProcess process, Action commited)
        {
            _process = process;
            _commited = commited;
        }

        private bool _isRefreshing;

        public async UniTask RefreshAsync()
        {
            if (_isRefreshing)
            {
                return;
            }

            _isRefreshing = true;
            _stagedFiles.Clear();
            _unstagedFiles.Clear();
            _untrackedFiles.Clear();
            var status = await _process.Async("status -uall");
            var newFiles = status.Split('\n');
            var checkStatus = "";
            foreach (var line in newFiles)
            {
                if (line.Contains("Changes to be committed:"))
                {
                    checkStatus = "staged";
                    continue;
                }

                if (line.Contains("Changes not staged for commit:"))
                {
                    checkStatus = "unstaged";
                    continue;
                }

                if (line.Contains("Untracked files:"))
                {
                    checkStatus = "untracked";
                    continue;
                }

                if (line.StartsWith("\t"))
                {
                    var trimedLine = line.Trim();
                    switch (checkStatus)
                    {
                        case "staged":
                            _stagedFiles.Add(trimedLine);
                            break;
                        case "unstaged":
                            _unstagedFiles.Add(trimedLine);
                            break;
                        case "untracked":
                            _untrackedFiles.Add(trimedLine.Trim());
                            break;
                    }
                }
            }

            _isRefreshing = false;
        }

        private readonly List<UniTask> _drawTaskList = new();

        public async UniTask DrawStatusAsync()
        {
            if (_drawTaskList.Count > 0)
            {
                return;
            }

            GUILayout.Label("コミット");
            {
                MornGitUtil.Indent(() =>
                {
                    MornGitUtil.H(() =>
                    {
                        if (GUILayout.Button("Refresh", GUILayout.Width(100), GUILayout.ExpandHeight(true)))
                        {
                            _drawTaskList.Add(default);
                        }

                        MornGitUtil.V(() =>
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                _commitMessage = GUILayout.TextField(_commitMessage, GUILayout.Height(40));
                                var anyStaged = _stagedFiles.Count > 0;
                                var canCommit = anyStaged && !string.IsNullOrEmpty(_commitMessage);
                                MornGitUtil.EnableColor(canCommit, Color.green, () =>
                                {
                                    if (GUILayout.Button("Commit", GUILayout.Width(100), GUILayout.Height(40)))
                                    {
                                        _drawTaskList.Add(_process.Async($"commit -m \"{_commitMessage}\""));
                                        _commitMessage = "";
                                        _commited();
                                    }
                                });
                            }

                            MornGitUtil.Box(() =>
                            {
                                GUILayout.Label($"Staged Files({_stagedFiles.Count})");
                                MornGitUtil.Indent(() =>
                                {
                                    foreach (var file in _stagedFiles)
                                    {
                                        var fileName = file.Split(':')[1].Trim();
                                        RestoreFile(file, fileName);
                                    }
                                });
                            });
                            MornGitUtil.Box(() =>
                            {
                                GUILayout.Label($"Unstaged Files({_unstagedFiles.Count})");
                                MornGitUtil.Indent(() =>
                                {
                                    foreach (var file in _unstagedFiles)
                                    {
                                        var fileName = file.Split(':')[1].Trim();
                                        AddFile(true, file, fileName);
                                    }
                                });
                            });
                            MornGitUtil.Box(() =>
                            {
                                GUILayout.Label($"Untracked Files({_untrackedFiles.Count})");
                                MornGitUtil.Indent(() =>
                                {
                                    foreach (var file in _untrackedFiles)
                                    {
                                        AddFile(false, file, file);
                                    }
                                });
                            });
                        });
                    });
                });
            }
            if (_drawTaskList.Count > 0)
            {
                foreach (var task in _drawTaskList)
                {
                    task.Forget();
                }

                await RefreshAsync();
                _drawTaskList.Clear();
            }
        }

        private void RestoreFile(string file, string fileName)
        {
            using (new GUILayout.HorizontalScope())
            {
                var color = GUI.color;
                GUI.color = Color.green;
                GUILayout.Label($"{file}");
                GUI.color = color;
                if (GUILayout.Button("Restore", GUILayout.Width(100)))
                {
                    _drawTaskList.Add(_process.Async($"restore --staged \"{fileName}\""));
                }
            }
        }

        private void AddFile(bool isTracked, string file, string fileName)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label($"{file}");
                if (isTracked)
                {
                    if (GUILayout.Button("Revert", GUILayout.Width(70)))
                    {
                        _drawTaskList.Add(CheckoutAsync(fileName));
                    }
                }
                else
                {
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        _drawTaskList.Add(RemoveAsync(fileName));
                    }
                }

                if (GUILayout.Button("Add", GUILayout.Width(100)))
                {
                    _drawTaskList.Add(_process.Async($"add \"{fileName}\""));
                }
            }
        }

        private async UniTask CheckoutAsync(string fileName)
        {
            await _process.Async($"checkout -- \"{fileName}\"");
            AssetDatabase.Refresh();
        }

        private async UniTask RemoveAsync(string fileName)
        {
            await _process.Async($"clean -df \"{fileName}\"");
            AssetDatabase.Refresh();
        }
    }
}