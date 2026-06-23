using UnityEngine;
using DG.Tweening;

public class testCube : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.DOMove(Vector3.up, 5);
        transform.DOScale(Vector3.one * 3, 5);
        transform.DORotate(Vector3.forward, 5);

        Material mat = GetComponent<MeshRenderer>().material;
        mat.DOColor(Color.cyan, 5);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
