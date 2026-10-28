using UnityEngine;
using UnityEngine.SceneManagement;

namespace FortunerCity.Core
{
    /// <summary>
    /// Boot scene → loads CityDrive.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        public string nextScene = "CityDrive";

        void Start()
        {
            SceneManager.LoadScene(nextScene);
        }
    }
}
