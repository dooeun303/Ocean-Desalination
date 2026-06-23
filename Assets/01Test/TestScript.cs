using UnityEngine;

public class TestScript : MonoBehaviour
{
    public Rigidbody rb;
    public float speed = 5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 dir = new Vector3(h, 0, v);
        rb.MovePosition(transform.position + dir * speed * Time.fixedDeltaTime);

        //transform.position += dir * speed * Time.deltaTime;

        //transform.Translate(0, 0, 1 * speed* Time.deltaTime, Space.World);

        if (dir != Vector3.zero)
        {
            //transform.rotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), 20f * Time.deltaTime);
        }

    }

    private void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name + " 범위 안에 들어왔다!");
    }

    private void OnTriggerStay(Collider other)
    {
        Debug.Log(other.gameObject.name + " 범위 안에 있다!");
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log(other.gameObject.name + " 범위 밖으로 나갔다!");
    }
}
