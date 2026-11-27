using UnityEngine;

public static class VectorUtility  
{
    /// <summary>
    /// Переводит мировой вектор движения в пространство относительно ориентации.
    /// Если move == orientation → вернёт Vector3.forward (0, 0, 1)
    /// </summary>
    public static Vector3 WorldToOrientationSpace(Vector3 move, Vector3 orientation)
    {
        var worldUp = Vector3.up;
    
        // 1. Строим кватернион поворота: "куда смотрит наша ориентация"
        Quaternion orientationRot = Quaternion.LookRotation(orientation, worldUp);
    
        // 2. Инвертируем поворот и применяем к вектору движения
        // Это "отменяет" поворот ориентации, переводя move в локальные координаты
        return Quaternion.Inverse(orientationRot) * move;
    }
  
}
