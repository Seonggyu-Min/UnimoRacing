using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyPanelManager : MonoBehaviour
{
    // === UI 컴포넌트 ===
    [SerializeField] private TMP_Text partyCountText;
    [SerializeField] private TMP_Text noMemberText;
    [SerializeField] private Image partyPanelImage; // 파티원 목록을 감싸는 패널의 Image 컴포넌트

    // === Firebase 관련 변수 ===
    private DatabaseReference _partyMembersRef;
    private string _currentPartyId;

    public void SetPartyId(string partyId)
    {
        // 이전 파티에 대한 리스너가 있다면 제거
        if (_partyMembersRef != null)
        {
            _partyMembersRef.ValueChanged -= OnPartyMembersChanged;
        }

        _currentPartyId = partyId;

        if (string.IsNullOrEmpty(_currentPartyId))
        {
            // 파티가 없는 상태 (솔로)
            UpdateUIState(0);
            return;
        }

        // 새로운 파티 경로에 리스너 설정
        _partyMembersRef = FirebaseDatabase.DefaultInstance.GetReference($"parties/{_currentPartyId}/members");
        _partyMembersRef.ValueChanged += OnPartyMembersChanged;
    }

    private void OnDestroy()
    {
        // 스크립트가 파괴될 때 리스너 제거
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
        // 파티원 수에 따라 UI 상태 변경
        if (memberCount > 0)
        {
            // 파티원이 있을 때
            partyCountText.text = $"{memberCount}/4";

            // Image 컴포넌트의 color 속성을 직접 변경합니다.
            if (partyPanelImage != null)
            {
                partyPanelImage.color = Color.green;
            }

            partyCountText.gameObject.SetActive(true);
            noMemberText.gameObject.SetActive(false);
        }
        else
        {
            // 파티원이 없을 때 (나 혼자일 때)
            // Image 컴포넌트의 color 속성을 직접 변경합니다.
            if (partyPanelImage != null)
            {
                partyPanelImage.color = Color.white;
            }

            partyCountText.gameObject.SetActive(false);
            noMemberText.gameObject.SetActive(true);
        }
    }
}
