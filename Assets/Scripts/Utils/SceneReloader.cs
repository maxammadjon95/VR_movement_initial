using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRBase
{
    /// <summary>
    /// Reloads the current scene. Hook Reload() to a UI Button's OnClick.
    /// The scene must be in the Build Profiles scene list, otherwise it cannot be loaded in a build.
    /// </summary>
    public class SceneReloader : MonoBehaviour
    {
        public void Reload()
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }
    }
}
