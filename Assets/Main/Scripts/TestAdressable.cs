using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class TestAdressable : MonoBehaviour
{
    [SerializeField]
    AssetReference asset;
    // Start is called before the first frame update
    void Start()
    {
        asset.InstantiateAsync(transform.position, Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
