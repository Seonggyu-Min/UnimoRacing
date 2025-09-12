using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyUIManager : MonoBehaviour
{
    public static PartyUIManager Instance { get; private set; }

    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject filledState;
    [SerializeField] private Transform partyMemberListParent;
    [SerializeField] private GameObject partyMemberSlotPrefab;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowEmpty()
    {
        emptyState.SetActive(true);
        filledState.SetActive(false);
    }

    public void ShowFilled()
    {
        emptyState.SetActive(false);
        filledState.SetActive(true);
    }

    public void AddPartyMember(string nickname, string levelText, Sprite icon)
    {
        ShowFilled();

        GameObject slot = Instantiate(partyMemberSlotPrefab, partyMemberListParent);
        var texts = slot.GetComponentsInChildren<TMP_Text>();
        texts[0].text = nickname;
        texts[1].text = levelText;
        var img = slot.GetComponentInChildren<Image>();
        img.sprite = icon;
    }
}