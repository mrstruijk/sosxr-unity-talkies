using System.Collections;
using UnityEngine;
using UnityEngine.Events;


namespace SOSXR.Talkies
{
    [RequireComponent(typeof(PinController))]
    public class PinTimerWithEvents : MonoBehaviour
    {

        [SerializeField][Range(1, 5000)][Tooltip("ms")] private int m_toggleTime = 150;
        [SerializeField][Range(0, 25)][Tooltip("GPIO")] private int m_pin = 16;

        [SerializeField][Range(0, 1000)][Tooltip("ms")] private int m_delay;

        [SerializeField] private UnityEvent<int> m_beforeDelay;
        [SerializeField] private UnityEvent<int> m_onTogglePin;
        [SerializeField] private UnityEvent<int> m_afterDelay;


        [SerializeField][HideInInspector] private PinController _controller;
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

            var delaySec = m_delay / (float)1000;
            m_beforeDelay?.Invoke(m_delay);

            yield return new WaitForSeconds(delaySec);

            _controller.SetPin(m_pin, true);
            var toggleTimeSec = m_toggleTime / (float)1000;

            m_onTogglePin?.Invoke(m_toggleTime);

            yield return new WaitForSeconds(toggleTimeSec);

            _controller.SetPin(m_pin, false);

            yield return new WaitForSeconds(delaySec);
            m_afterDelay?.Invoke(m_delay);

            _toggleCoroutine = null;
        }


        private void OnDisable()
        {
            _controller.SetPin(m_pin, false);

            StopAllCoroutines();
        }
    }
}
