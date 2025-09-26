using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class BadWordFinder : MonoBehaviour
    {
        [SerializeField] private string path = "badwords";

        private HashSet<string> _words = new();
        private TextAsset _loadedAsset;


        private void Start() => LoadAsset();
        private void OnDisable() => UnloadAsset();


        private void LoadAsset()
        {
            if (!string.IsNullOrEmpty(path))
            {
                _loadedAsset = Resources.Load<TextAsset>(path);
                if (_loadedAsset == null)
                {
                    Debug.LogWarning("금칙어 파일을 찾을 수 없음.");
                    return;
                }

                _words.Clear();

                string[] lines = _loadedAsset.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string word = line.Trim();
                    if (!string.IsNullOrEmpty(word))
                        _words.Add(word);
                }

                Debug.Log($"[BadWordFinder] 금칙어 {_words.Count}개 로드됨");
            }
        }

        private void UnloadAsset()
        {
            if (_loadedAsset != null)
            {
                Resources.UnloadAsset(_loadedAsset);
                _loadedAsset = null;
                Debug.Log("[BadWordFinder] TextAsset 언로드 완료");
            }
        }


        public bool ContainsBadWord(string nickname)
        {
            if (nickname == null) return false;

            foreach (string word in _words)
            {
                if (nickname.Contains(word))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
