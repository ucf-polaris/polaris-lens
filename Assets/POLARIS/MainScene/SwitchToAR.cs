using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace POLARIS
{
    public class SwitchToAR : MonoBehaviour
    {
        private UIDocument _buttonDocument;
        private Button _button;
        private AsyncOperation _sceneAsync;

        // Start is called before the first frame update
        private void Start()
        {
            _buttonDocument = GetComponent<UIDocument>();
            if (_buttonDocument == null)
            {
                print("No Button Doc Found!");
            }

            _button = _buttonDocument.rootVisualElement.Q("SwitchButton") as Button;
            if (_button != null)
            {
                print("Found button B)");
            }

            _button.RegisterCallback<ClickEvent>(OnButtonClick);
        }

        private void OnButtonClick(ClickEvent clickEvent)
        {
            print("Clicked da button");
            GoToScene("Geospatial");
        }
        
        private void GoToScene(string sceneName)
        {
            StartCoroutine(LoadScene(sceneName));
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            SceneManager.LoadSceneAsync(sceneName);

            SceneManager.sceneLoaded += (newScene, mode) =>
            {
                SceneManager.SetActiveScene(newScene);
            };

            yield return null;
        }
    }
}
