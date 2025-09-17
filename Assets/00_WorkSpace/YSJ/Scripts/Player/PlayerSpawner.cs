using Photon.Pun;
using System.Collections;
using UnityEngine;
using YSJ.Util;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Injecter")]
    [SerializeField] private bool _isUseInjecter = false;
    
    [Header("Config")]
    [SerializeField] private bool _isStartDirectSpawn = false;

    private GameObject _baseGO;

    private bool IsSpawnable => PhotonNetwork.InRoom; // 룸 안에 들어와 있을 때

    private void Start()
    {
        if (_isStartDirectSpawn)
        {
            StartCoroutine(CO_PlayerSpanwe());
        }
        else
        {
            InGameManager.Instance.OnRaceState_LoadPlayers -= OnSpawnAction;
            InGameManager.Instance.OnRaceState_LoadPlayers += OnSpawnAction;
        }
    }

    private void OnSpawnAction()
    {
        StartCoroutine(CO_PlayerSpanwe());
    }

    private IEnumerator CO_PlayerSpanwe()
    {
        _baseGO = Resources.Load<GameObject>(LoadPath.PLAYER_BASE_PREFAB_PATH);
        if (_baseGO == null)
        {
            this.PrintLog("플레이어 베이스 프리팹이 없습니다.");
            yield break;
        }

        this.PrintLog("플레이어, 룸에 들어와 있는지 확인중");
        while (!IsSpawnable || !TrackPathRegistry.Instance.IsInit)
        {
            yield return null;
        }

        this.PrintLog("플레이어, 데이터 로드");
        TrySpawnLocal();
        yield break;
    }

    private void TrySpawnLocal()
    {
        var me = PhotonNetwork.LocalPlayer;

        PlayerSpawnDataInjecter injecter = GetComponent<PlayerSpawnDataInjecter>();
        PlayerSpawnData data = null;

        _isUseInjecter = UseInjecter(injecter);
        data = injecter.GetData();

        // ID ===========================================================================
        // 아이디 관련 처리
        int userCharacterID = -1;
        int userKartID = -1;

        if (_isUseInjecter)
        {
            userCharacterID = data.CharacterID;
            userKartID = data.KartID;
        }
        else
        {
            /*
            // 서버에서 현 로컬 플레이어의 IDs 가지고 오기
            DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedKart(me.UserId), GetSuccessCharacterID, GetErrorCharacterID);
            DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedUnimo(me.UserId), GetSuccessKartID, GetErrorKartID);
            */
        }

        // 아이디 값 이상하게 들어왔을 때에 대한 예외 처리
        if (userCharacterID == -1 || userKartID == -1)
        {
            userCharacterID = (userCharacterID == -1) ? 20001 : userCharacterID;
            userKartID = (userKartID == -1) ? 10001 : userKartID;
            this.PrintLog($"ID 이상 발견 > 베이스 아이디로 처리\n" +
                $"(userCharacterID = {userCharacterID} / userKartID = {userKartID})", LogType.Warning);
        }

        // SO ===========================================================================
        // TODO: 나중에 로드 부분만 어드레서블 오브젝트 로드 변경
        UnimoCharacterSO    characterSO = null;
        UnimoKartSO         kartSO = null;

        if (_isUseInjecter)
        {
            characterSO = data.InjectCharacterSO;
            kartSO = data.InjectKartSO;
            if (characterSO == null || kartSO == null)
            {
                characterSO = Resources.Load<UnimoCharacterSO>($"{LoadPath.PLAYER_UNIMO_CHARACTER_SO}_{userCharacterID}");
                kartSO = Resources.Load<UnimoKartSO>($"{LoadPath.PLAYER_UNIMO_KART_SO}_{userKartID}");
            }
        }
        else
        {
            characterSO = Resources.Load<UnimoCharacterSO>($"{LoadPath.PLAYER_UNIMO_CHARACTER_SO}_{userCharacterID}");
            kartSO = Resources.Load<UnimoKartSO>($"{LoadPath.PLAYER_UNIMO_KART_SO}_{userKartID}");
        }

        if (characterSO == null || kartSO == null)
        {
            this.PrintLog($"SO 이상 발견 \n" +
                $"(characterSO = {characterSO == null}, {characterSO} / kartSO = {kartSO == null}, {kartSO})", LogType.Error);
            return;
        }

        this.PrintLog($"플레이어 필요 데이터 로드 완료\n" +
            $"로드 시도자 > [{me} / {me.ActorNumber} / {UnityUtilEx.GetPlayerRoomIndex(me)}]\n" +
            $"characterSO = {characterSO == null}, {characterSO.name}\n" +
            $"kartSO = {kartSO == null}, {kartSO.name}");


        object[] instData = {userCharacterID, userKartID};
        PhotonNetwork.Instantiate(_baseGO.name, Vector3.zero, Quaternion.identity, 0, instData);
    }

    private bool UseInjecter(PlayerSpawnDataInjecter injecter)
    {
        if (_isUseInjecter == false) return false;

        return (injecter != null && injecter.GetData() != null);
    }

    // 네트워크
    /*
    
    // onSuccess
    private void GetSuccessCharacterID(DataSnapshot snapShot)
    {
        characterID = PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_CHARACTER_ID;
        _isSetCharacterID = true;
        if (!int.TryParse(snapShot.Value.ToString(), out characterID))
            return;
    }
    private void GetSuccessKartID(DataSnapshot snapShot)
    {
        kartID = PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_KART_ID;
        _isSetkartID = true;
        if (!int.TryParse(snapShot.Value.ToString(), out kartID))
            return;
    }

    // onError(연결이 끊키거나, 데이터를 가지고 오기를 실패헀을 때, 엔진 제차 에러거나 등)
    private void GetErrorCharacterID(string snapShot)
    {
        characterID = PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_CHARACTER_ID;
        _isSetCharacterID = true;
    }
    private void GetErrorKartID(string snapShot)
    {
        kartID = PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_KART_ID;
        _isSetkartID = true;
    }

    */
}
