using System.Collections;
using UnityEngine;
using ButtonAttribute = SOSXR.SeaShark.ButtonAttribute;


namespace SOSXR.Talkies
{
    /// <summary>
    ///     Drives a GPIO pin HIGH for a short configurable duration, then LOW — a single pulse.
    ///     Requires a <see cref="PinController"/> on the same GameObject.
    ///     If a pulse is already in progress when <c>ToggleOnOff</c> is called again, the
    ///     running coroutine is cancelled and the pin is driven LOW.
    /// </summary>
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


        /// <summary>
        ///     Starts a one-shot pulse on the configured pin: HIGH for <c>m_toggleTime</c> seconds,
        ///     then LOW. If a pulse is already active, stops it and drives the pin LOW instead.
        /// </summary>
        [Button]
        private void ToggleOnOff()
        {
            if (_toggleCoroutine != null)
            {
                StopCoroutine(_toggleCoroutine);

                _controller.SetPin(m_pin, false);

                Debug.LogWarning("We already had a toogler running. Will turn that toggler off, but not continue from here. Toggle again to resume functionality.");

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
}
