/// <summary>
/// Интерфейс для объектов (юнитов/аватаров), поддерживающих переключение визуального вида
/// при смене режима камеры (первое лицо / третье лицо).
/// </summary>
public interface IViewModeSwitchable
{
    void SetFirstPersonView();
    void SetThirdPersonView();
}
