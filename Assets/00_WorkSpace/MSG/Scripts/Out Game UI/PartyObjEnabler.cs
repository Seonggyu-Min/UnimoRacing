using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class PartyObjEnabler : MonoBehaviour
    {
        [SerializeField] private GameObject _partyObj;

        private void Start()
        {
            _partyObj.SetActive(false);
        }

        private void OnEnable()
        {
            PartyService.Instance.OnPartyChanged += OnPartyChanged;
        }

        private void OnDisable()
        {
            if (PartyService.Instance != null)
            {
                PartyService.Instance.OnPartyChanged -= OnPartyChanged;
            }
        }

        private void OnPartyChanged()
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
}
