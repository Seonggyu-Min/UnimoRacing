using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    [CreateAssetMenu(fileName = "DialogueSO", menuName = "ScriptableObjects/DialogueSO")]
    public class DialogueSO : ScriptableObject
    {
        [SerializeField] private List<DialogueBox> dialogueBoxes = new();

        public List<DialogueBox> DialogueBoxes => dialogueBoxes;
    }

    [Serializable]
    public class DialogueBox
    {
        [SerializeField] private int _dialogueIndex;
        [SerializeField][TextArea] private string _text;

        public int DialogueIndex => _dialogueIndex;
        public string Text => _text;
    }
}
