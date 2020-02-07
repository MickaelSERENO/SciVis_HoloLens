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
            Vector3 euler = transform.rotation.eulerAngles;
            euler.z = 0;
            transform.rotation = Quaternion.Euler(euler);
        }
    }
}