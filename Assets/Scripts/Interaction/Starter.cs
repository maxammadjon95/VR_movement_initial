using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Starter : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private Button _button;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private GameObject _activeObject;

    private float _delay = 0.01f;

    private void OnEnable()
    {
        _button.onClick.AddListener(StartLoading);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(StartLoading);
    }

    private void StartLoading()
    {
        StartCoroutine(LoadCoroutine());
    }

    private IEnumerator LoadCoroutine()
    {
        for(float i=0; i<=1; i+= _delay)
        {
            yield return new WaitForSeconds(_delay);
            _image.fillAmount = i;
        }
        _group.gameObject.SetActive(false);
        _activeObject.SetActive(true);
    }

}
