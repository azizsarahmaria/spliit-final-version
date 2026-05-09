using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform playerTransform;
    public Transform camTransform;
    public float offsetX = 1;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Vector3 playerPosition = GameObject.FindWithTag("Player").transform.position;
        playerTransform = GameObject.FindWithTag("Player").transform;
    
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 cameraposition = this.transform.position; // 0 0 -10
        cameraposition.x = playerTransform.position.x + offsetX; // -2.69 0 -10          //every frame i update the cameras position to match
        this.transform.position = cameraposition;

    }
}
