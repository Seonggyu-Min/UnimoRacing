using Photon.Pun;
using UnityEngine;

public class AutoDespawn : MonoBehaviourPun
{
    [Tooltip("이 시간이 지나면 오너가 PhotonNetwork.Destroy를 호출합니다.")]
    public float lifeSeconds = 2f;

    private void OnEnable()
    {
        if (lifeSeconds > 0f)
            StartCoroutine(Despawn());
    }

    private System.Collections.IEnumerator Despawn()
    {
        yield return new WaitForSeconds(lifeSeconds);

        if (!this) yield break;

        if (photonView != null && photonView.IsMine)
            PhotonNetwork.Destroy(gameObject); // 전 클라 제거
        else
            Destroy(gameObject); // 안전망
    }
}
