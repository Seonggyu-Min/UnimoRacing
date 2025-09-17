using System;
using UnityEngine;

[Serializable]
public class PlayerSpawnData
{
    public int CharacterID = -1;
    public int KartID = -1;

    [Header("Default Inject SO")]
    [SerializeField] private UnimoCharacterSO injectDefaultCharacterSO;
    [SerializeField] private UnimoKartSO injectDefaultInJectKartSO;

    public UnimoCharacterSO InjectCharacterSO
    {
        get
        {
            if (injectDefaultCharacterSO == null)
                injectDefaultCharacterSO = Resources.Load<UnimoCharacterSO>($"{LoadPath.PLAYER_UNIMO_CHARACTER_SO}_{0}");

            return injectDefaultCharacterSO;
        }
    }

    public UnimoKartSO InjectKartSO
    {
        get
        {
            if (injectDefaultInJectKartSO == null)
                injectDefaultInJectKartSO = Resources.Load<UnimoKartSO>($"{LoadPath.PLAYER_UNIMO_KART_SO}_{0}");

            return injectDefaultInJectKartSO;
        }
    }
}
