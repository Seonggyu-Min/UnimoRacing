using UnityEngine;
using Photon.Pun;

public class KartController : MonoBehaviour, IPunObservable
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float turnSpeed = 100f;

    private PhotonView photonView;

    // 네트워크 동기화용
    private Vector3 networkPos;
    private Quaternion networkRot;
    private float lerpSpeed = 10f; // 보간 속도

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        networkPos = transform.position;
        networkRot = transform.rotation;
    }

    private void Update()
    {
        if (photonView.IsMine) // 내가 조종하는 카트
        {
            HandleInput();
        }
        else // 다른 유저의 카트 > 보간 이동
        {
            transform.position = Vector3.Lerp(transform.position, networkPos, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRot, Time.deltaTime * lerpSpeed);
        }
    }

    private void HandleInput()
    {
        // 나중에 입력 추가 예정
        Vector3 move = transform.forward * speed * Time.deltaTime;
        transform.position += move;
    }

    /// <summary>
    /// 포톤 직렬화 > 위치/회전 전송
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // 내가 보내는 쪽
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else // 다른 사람이 보낸 걸 받는 쪽
        {
            networkPos = (Vector3)stream.ReceiveNext();
            networkRot = (Quaternion)stream.ReceiveNext();
        }
    }
}
