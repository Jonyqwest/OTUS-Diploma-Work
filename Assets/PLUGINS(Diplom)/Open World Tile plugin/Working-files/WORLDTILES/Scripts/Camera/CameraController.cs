using System.Globalization;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private float moveSpeedMetersPerSec = 100f;
    [SerializeField]
    private float verticalSpeedUnity = 10f;
    [SerializeField]
    private float fastMultiplier = 2f;

     

    private WorldPosition cameraWorldPos;
    [SerializeField]
    private float realHeightMeters;
    private float lastRealHeight;

    

    void Start() {
        cameraWorldPos = PlayerWorldStat.GetCurrentWorldPosition();

        var _camPos = cameraWorldPos.ToUnityPosition();
        transform.position = _camPos;
    }


    void Update()
    {
        PlayerWorldStat.SetCurrentWorldPosition(cameraWorldPos);

        if (lastRealHeight != realHeightMeters)
        {
            cameraWorldPos.Height = realHeightMeters;
            lastRealHeight = realHeightMeters;
            //Debug.Log("Установлена высота -> " + realHeightMeters + "метров");
            transform.position = cameraWorldPos.ToUnityPosition();
            
        }
        //cameraWorldPos.UpdateFromUnityPosition(transform.position);
        HandleMovement();

        
        if (Input.GetKeyDown(KeyCode.C))
        {
            var latLon = cameraWorldPos.ToLatLon();
            string googleCoord = $"{latLon.lat:F6},{latLon.lon:F6}";
            Debug.Log($"Google Maps coords: {googleCoord}");
        }
    }

    void HandleMovement()
    {
        float worldScaleFactor = TileStat.BaseTileSize / 300f;
        
        var heightSpeed = verticalSpeedUnity * TileStat.ToUnit * worldScaleFactor; 
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
            cameraWorldPos.MoveByMeters(moveMeter);
            transform.position = cameraWorldPos.ToUnityPosition();
        }
    }

    bool TryParseLatLon(string input, out double lat, out double lon)
    {
        lat = 0; lon = 0;

        var parts = input.Split(',');
        if (parts.Length != 2)
        {   Debug.LogWarning("[CameraController] Geo строка = '"+input + "' не содержит разделителя = ','");
            return false; }
        var res1 = double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out lat);
        var res2 = double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out lon);
        if (!res1 || !res2)
        {
            Debug.Log("[CameraController] Попытка распарсить Geo строку = '" + input + "'");
            Debug.LogWarning("[CameraController] Parse - [ Lat:" + res1 + " / Lon:" + res2 + " ]"); ;
        }
        return res1 && res2;
    }

    void OnApplicationQuit()
    {   //при выходе сохраняем позицию
        WorldPosLoader.Save(cameraWorldPos);
    }
}
