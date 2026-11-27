using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testMoveController : MonoBehaviour
{
    [SerializeField]
    private float moveSpeedMetersPerSec = 1f;
    [SerializeField]
    private float verticalSpeedUnity = 10f;
    [SerializeField]
    private float fastMultiplier = 2f;


    private float realHeightMeters;
    private float lastRealHeight;
    
    float worldScaleFactor = TileStat.BaseTileSize / 100f;

    void Update()
    {
        HandleMovement(); 
    }

    void HandleMovement()
    {
        var heightSpeed = verticalSpeedUnity * TileStat.ToUnit + worldScaleFactor;
        //ускорение
        float speed = moveSpeedMetersPerSec * worldScaleFactor;

        if (Input.GetKey(KeyCode.LeftShift)) speed *= fastMultiplier;

        //движение по плоскости
        Vector3 movePlane = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) movePlane += new Vector3(0, 0, 1);
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) movePlane += new Vector3(0, 0, -1);
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) movePlane += new Vector3(-1, 0, 0);
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) movePlane += new Vector3(1, 0, 0);

        Vector3 moveHeight = Vector3.zero;
        //перемещение по высоте
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space))
            moveHeight += new Vector3(0, 1, 0);
        if (Input.GetKey(KeyCode.Q))
            moveHeight += new Vector3(0, -1, 0);

        if (movePlane != Vector3.zero || moveHeight != Vector3.zero)
        {
            Vector3 moveVector = (movePlane + moveHeight * heightSpeed) * speed * Time.deltaTime;
            Vector3 moveMeter = moveVector * TileStat.ToMeter;
            transform.position = transform.position + moveMeter;
        }
    }
}
