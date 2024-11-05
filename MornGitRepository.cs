using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MornGit
{
    public class MornGitRepository
    {
        private readonly string _url;
        private readonly string _repositoryName;
        private readonly MornGitProcess _process;
        private readonly MornGitBranch _branch;
        private readonly MornGitStatus _status;
        private readonly MornGitHistory _history;
        private bool IsOpen
        {
            get => MornGitUtil.GetFlag(_process.Path, "isOpen", false);
            set => MornGitUtil.SetFlag(_process.Path, "isOpen", value);
        }

        public MornGitRepository(MornGitProcess process, string url, string repositoryName)
        {
            _process = process;
            _url = url;
            _repositoryName = repositoryName;
            _branch = new MornGitBranch(_process, OnCheckout);
            _history = new MornGitHistory(_process, _url, ToRefreshStatus);
            _status = new MornGitStatus(_process, OnCommit);
        }

        public async UniTask RefreshAsync()
        {
            await _branch.RefreshAsync();
            await _status.RefreshAsync();
            await _history.RefreshAsync();
        }

        private void OnCheckout()
        {
            _status.RefreshAsync();
            _history.RefreshAsync();
        }

        private void ToRefreshStatus()
        {
            _status.RefreshAsync();
        }

        private void OnCommit()
        {
            _branch.RefreshAsync();
            _history.RefreshAsync();
        }

        public void OnGUI()
        {
            MornGitUtil.Box(() =>
            {
                DrawRepositoryInfo();
                if (IsOpen)
                {
                    MornGitUtil.IndentBox(() =>
                    {
                        _branch.DrawBranchAsync().Forget();
                        DrawLine();
                        _status.DrawStatusAsync().Forget();
                        DrawLine();
                        _history.DrawHistoryAsync().Forget();
                    });
                }
            });
        }

        private void DrawLine()
        {
            GUILayout.Space(-2);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(4));
            GUILayout.Space(-2);
        }

        private void DrawRepositoryInfo()
        {
            using (new GUILayout.HorizontalScope())
            {
                var buttonName = IsOpen ? "▼" : "▶";
                if (GUILayout.Button(buttonName, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    IsOpen = !IsOpen;
                }

                var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 24 };
                var displayName = $"{_repositoryName} ({_branch.CurrentBranch})";
                if (_status.ChangeCount > 0)
                {
                    displayName += $" - {_status.ChangeCount} changed.";
                }

                GUILayout.Label(displayName, labelStyle, GUILayout.ExpandWidth(true), GUILayout.Height(30));
                if (GUILayout.Button("Folder", GUILayout.Width(60), GUILayout.Height(30)))
                {
                    Process.Start(_process.Path);
                }

                if (GUILayout.Button("Web", GUILayout.Width(50), GUILayout.Height(30)))
                {
                    Application.OpenURL(_url);
                }
            }
        }
    }
}