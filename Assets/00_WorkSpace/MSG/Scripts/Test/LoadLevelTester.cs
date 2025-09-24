using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace MSG
{
    public class LoadLevelTester : MonoBehaviour
    {
        [SerializeField][Range(0, 4)] int _level;


        [Button("Load Level")]
        private void LoadLevelByField()
        {
            SceneManager.LoadScene(_level);
        }
    }
}
