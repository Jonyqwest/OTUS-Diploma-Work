using System.Globalization;
using UnityEngine;

public class WorldStartPosition : MonoBehaviour
{
     
    [Header("Начальная позиция (широта, долгота)")]
    public bool isStartingGeoCoord = true;
    public string GeoCoord = "18.756600781414612, 8.55610012239133";
    [SerializeField]
    private float realHeightMeters = 200f;

    private WorldPosition cameraWorldPos;
 


    void Awake() {
        bool isValidGeoCoored = TryParseLatLon(GeoCoord, out double lat, out double lon);
        string errorGeoCoord = "";
        if (!isValidGeoCoored)
            errorGeoCoord = "Некорректная строка координат.";

        if (isStartingGeoCoord & isValidGeoCoored)
        {
            //загружаем ссылку на точку
            PlayerWorldStat.SetCurrentWorldPosition(WorldPosition.FromLatLon(lat, lon, TileStat.BaseZoom, realHeightMeters));
           
            Debug.LogWarning("[WorldStartPosition] Перемещаемся в координату Lat=" + lat + "; Lon=" + lon + "; Zoom=" + TileStat.BaseZoom + " тайл[ " + cameraWorldPos + " ]");
        }
        else
        {

            if (WorldPosLoader.TryLoad(out WorldPosition loadedPos))
            {   //загружаем сохраненную позицию
                PlayerWorldStat.SetCurrentWorldPosition(loadedPos);
                realHeightMeters = PlayerWorldStat.GetCurrentWorldPosition().Height;
                transform.position = loadedPos.ToUnityPosition();
                Debug.LogWarning("[CameraController] " + errorGeoCoord + " Продолжаем с сохраненной координаты [ " + cameraWorldPos + " ]");
            }

            else
            {   //если нет ссылки на точку идем по дефаулту
                //PlayerWorldStat.currentPosition = WorldPosition.FromUnityPosition(transform.position, TileStat.BaseZoom);
                Debug.LogWarning("[CameraController] " + errorGeoCoord + " Начинаем по умолчанию с [ " + cameraWorldPos + " ]");

            }
        }
        cameraWorldPos = PlayerWorldStat.GetCurrentWorldPosition();

        var _camPos = cameraWorldPos.ToUnityPosition();
        transform.position = _camPos;
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
