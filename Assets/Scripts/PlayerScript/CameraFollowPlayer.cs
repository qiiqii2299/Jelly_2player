using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    public Transform target;
    public float followSpeed = 5f;
    public Vector3 v3;
    
    void Update()
    {
       if(target != null)
       {
           v3 = new Vector3(target.position.x, target.position.y, transform.position.z);
           transform.position = Vector3.Lerp(transform.position, v3, followSpeed * Time.deltaTime);
       }
    }
}
