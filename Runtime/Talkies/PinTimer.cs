using System.Collections;
using SOSXR.EnhancedLogger;
using UnityEngine;
using ButtonAttribute = SOSXR.SeaShark.ButtonAttribute;


[RequireComponent(typeof(PinController))]
public class PinTimer : MonoBehaviour
{
    [SerializeField] [HideInInspector] private PinController _controller;
    [SerializeField] [Range(0.15f, 5f)] private float m_toggleTime = 0.15f;
    [SerializeField] [Range(0, 25)] private int m_pin = 16;

    private Coroutine _toggleCoroutine;


    private void OnValidate()
    {
        if (_controller == null)
        {
            _controller = GetComponent<PinController>();
        }
    }


    [Button]
    private void ToggleOnOff()
    {
        if (_toggleCoroutine != null)
        {
            StopCoroutine(_toggleCoroutine);

            _controller.SetPin(m_pin, false);

            this.Warning("We already had a toogler running. Will turn that toggler off, but not continue from here. Toggle again to resume functionality.");

            _toggleCoroutine = null;

            return;
        }

        _toggleCoroutine = StartCoroutine(ToggleOnOffCR());
    }


    private IEnumerator ToggleOnOffCR()
    {
        if (!Application.isPlaying)
        {
            yield break;
        }

        _controller.SetPin(m_pin, true);

        yield return new WaitForSeconds(m_toggleTime);

        _controller.SetPin(m_pin, false);

        _toggleCoroutine = null;
    }


    private void OnDisable()
    {
        _controller.SetPin(m_pin, false);

        StopAllCoroutines();
    }
}