using VContainer.Unity;

namespace _Project.Develop.Infrastructure.EntryPoints
{
    public class BootstrapEntryPoint : IStartable
    {
        public void Start()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0)
                UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        }
    }
}