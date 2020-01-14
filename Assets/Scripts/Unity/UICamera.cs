using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno
{
    public class UICamera : MonoBehaviour
    {
        void Start()
        {}

        void Update()
        {
            Quaternion q = transform.rotation;
            q.z = 0;
            transform.rotation = q.normalized;
        }
    }
}