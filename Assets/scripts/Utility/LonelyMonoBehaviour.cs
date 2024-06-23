using UnityEngine;

namespace Utility {
    public class LonelyMonoBehaviour<T> : MonoBehaviour where T : Component {
        private static T _instance;

        public static T Instance {
            get {
                if (_instance == null)
                    // see if there's already one of this in the scene, and if so, bind it to _instance
                    _instance = FindObjectOfType<T>();

                if (_instance == null) {
                    Debug.LogError(
                        $"LonelyMonoBehaviour {typeof(T)} is being called but hasn't been added to a game object in the scene"
                    );
                    return null;
                }

                // yay, we have an instance! carry on and execute methods and stuff
                return _instance;
            }
        }

        private void Awake() {
            //if _instance is populated immediately destroy yourself, you are useless and un-needed
            if (_instance != null && _instance != this) {
                DestroyImmediate(GetComponent<T>());
                return;
            }

            //if one of this doesn't already exist, become THE ONE
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
    }
}
