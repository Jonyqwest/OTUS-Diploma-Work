using Code.Data;

namespace Code.Infrastracture.Services.PersistentProgress
{
     public class PersistentProgressService: IPersistentProgressService
     {
          public PlayerProgress Progress { get; set; } = new PlayerProgress();
     }

     public interface IPersistentProgressService: IService
     {
          PlayerProgress Progress { get; set; }
     }
}