using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyPanelManager : MonoBehaviour
{
    // === UI ������Ʈ ===
    [SerializeField] private TMP_Text partyCountText;
    [SerializeField] private TMP_Text noMemberText;
    [SerializeField] private Image partyPanelImage; // ��Ƽ�� ����� ���δ� �г��� Image ������Ʈ

    // === Firebase ���� ���� ===
    private DatabaseReference _partyMembersRef;
    private string _currentPartyId;

    public void SetPartyId(string partyId)
    {
        // ���� ��Ƽ�� ���� �����ʰ� �ִٸ� ����
        if (_partyMembersRef != null)
        {
            _partyMembersRef.ValueChanged -= OnPartyMembersChanged;
        }

        _currentPartyId = partyId;

        if (string.IsNullOrEmpty(_currentPartyId))
        {
            // ��Ƽ�� ���� ���� (�ַ�)
            UpdateUIState(0);
            return;
        }

        // ���ο� ��Ƽ ��ο� ������ ����
        _partyMembersRef = FirebaseDatabase.DefaultInstance.GetReference($"parties/{_currentPartyId}/members");
        _partyMembersRef.ValueChanged += OnPartyMembersChanged;
    }

    private void OnDestroy()
    {
        // ��ũ��Ʈ�� �ı��� �� ������ ����
        if (_partyMembersRef != null)
        {
            _partyMembersRef.ValueChanged -= OnPartyMembersChanged;
        }
    }

    private void OnPartyMembersChanged(object sender, ValueChangedEventArgs args)
    {
        long memberCount = 0;
        if (args.Snapshot.Exists)
        {
            memberCount = args.Snapshot.ChildrenCount;
        }

        UpdateUIState(memberCount);
    }

    private void UpdateUIState(long memberCount)
    {
        // ��Ƽ�� ���� ���� UI ���� ����
        if (memberCount > 0)
        {
            // ��Ƽ���� ���� ��
            partyCountText.text = $"{memberCount}/4";

            // Image ������Ʈ�� color �Ӽ��� ���� �����մϴ�.
            if (partyPanelImage != null)
            {
                partyPanelImage.color = Color.green;
            }

            partyCountText.gameObject.SetActive(true);
            noMemberText.gameObject.SetActive(false);
        }
        else
        {
            // ��Ƽ���� ���� �� (�� ȥ���� ��)
            // Image ������Ʈ�� color �Ӽ��� ���� �����մϴ�.
            if (partyPanelImage != null)
            {
                partyPanelImage.color = Color.white;
            }

            partyCountText.gameObject.SetActive(false);
            noMemberText.gameObject.SetActive(true);
        }
    }
}
