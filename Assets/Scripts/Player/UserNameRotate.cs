using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserNameRotate : MonoBehaviour
{
    private Transform mainCameraTransform;
    private void Start()
    {
        mainCameraTransform = Camera.main.transform;
    }

    // Update is called once per frame
    private void LateUpdate()
    {
        transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward, mainCameraTransform.rotation * Vector3.up);
    }
}
