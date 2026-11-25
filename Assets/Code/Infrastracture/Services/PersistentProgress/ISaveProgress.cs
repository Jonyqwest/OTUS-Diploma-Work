using Code.Data;

namespace Code.Infrastracture.Services.PersistentProgress
{
    public interface ISaveProgressReader
    {
        void UpdateProgress(PlayerProgress progress);
    }
    public interface ISaveProgress: ISaveProgressReader
    {
        void LoadProgress(PlayerProgress progress);
    }
}