using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegisterMesh : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SSTDManager.Get.RegisterMesh(this.gameObject);
    }
}
