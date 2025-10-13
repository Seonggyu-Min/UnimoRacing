using MSG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyOutEnabler : MonoBehaviour
{
    [SerializeField] private GameObject _partyObj;

    private void Start()
    {
        _partyObj.SetActive(false);
    }

    private void OnEnable()
    {
        CheckPartyChanged();
        PartyService.Instance.OnPartyChanged += CheckPartyChanged;
    }

    private void OnDisable()
    {
        if (PartyService.Instance != null)
        {
            PartyService.Instance.OnPartyChanged -= CheckPartyChanged;
        }

        _partyObj.SetActive(false);
    }

    private void CheckPartyChanged()
    {
        if (PartyService.Instance.IsInParty)
        {
            _partyObj.SetActive(true);
        }
        else
        {
            _partyObj.SetActive(false);
        }
    }
}