using System;
using UnityEngine;

public class Collision3DAction : MonoBehaviour
{
    public Action<Collider> OnTriggerEnterAction;
    public Action<Collider> OnTriggerStayAction;
    public Action<Collider> OnTriggerExitAction;

    public Action<Collision> OnCollisionEnterAction;
    public Action<Collision> OnCollisionStayAction;
    public Action<Collision> OnCollisionExitAction;

    // Trigger
    private void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterAction?.Invoke(other);

    }
    private void OnTriggerStay(Collider other)
    {
        OnTriggerStayAction?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        OnTriggerExitAction?.Invoke(other);
    }

    // Collision
    private void OnCollisionEnter(Collision collision)
    {
        OnCollisionEnterAction?.Invoke(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        OnCollisionStayAction?.Invoke(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        OnCollisionExitAction?.Invoke(collision);
    }
}
