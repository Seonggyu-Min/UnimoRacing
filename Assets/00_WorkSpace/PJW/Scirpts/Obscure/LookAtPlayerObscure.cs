using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class LookAtPlayerObscure : MonoBehaviour
    {
        private Transform player;

        private void Start()
        {
            GameObject target = GameObject.FindGameObjectWithTag("Player");
            if (target != null)
            {
                player = target.transform;
            }
        }

        private void Update()
        {
            if (player == null) return;

            transform.LookAt(player);
        }
    }
}
