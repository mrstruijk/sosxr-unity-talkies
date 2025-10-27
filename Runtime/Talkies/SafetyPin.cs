using System.Collections;
using System.Collections.Generic;
using SOSXR.EnhancedLogger;
using UnityEngine;


public class SafetyPin : MonoBehaviour
{
    [SerializeField] [HideInInspector] private PinController _pinController;
    [SerializeField] [Range(1, 60)] private int m_onSeconds = 30;

    [SerializeField] private List<float> m_safetyPin = new();
    private readonly float _checkDelay = 1f;


    private void OnValidate()
    {
        if (_pinController == null)
        {
            _pinController = GetComponent<PinController>();
        }
    }


    private void Awake()
    {
        m_safetyPin.Clear();
        m_safetyPin = new List<float>(0);
    }


    private void OnEnable()
    {
        _pinController.OnPinSetEvent += StartTimer;
    }


    private void Start()
    {
        StartCoroutine(TimeCheckerCR());
    }


    private void StartTimer(int pinNumber, bool value)
    {
        if (m_safetyPin.Count <= pinNumber)
        {
            m_safetyPin.AddRange(new float[pinNumber + 1 - m_safetyPin.Count]);
        }

        if (!value)
        {
            m_safetyPin[pinNumber] = 0;

            return;
        }

        if (m_safetyPin[pinNumber] != 0)
        {
            return;
        }

        m_safetyPin[pinNumber] = Time.time + m_onSeconds;
    }


    private IEnumerator TimeCheckerCR()
    {
        for (;;)
        {
            yield return new WaitForSeconds(_checkDelay);

            for (var i = 0; i < m_safetyPin.Count; i++)
            {
                if (m_safetyPin[i] == 0)
                {
                    continue;
                }

                if (m_safetyPin[i] <= Time.time)
                {
                    this.Warning($"Setting pin {i} to LOW because it has been on longer than {m_onSeconds} seconds.");

                    _pinController.SetPin(i, false);
                    m_safetyPin[i] = 0;
                }
            }
        }
    }


    private void HardDisableAllKnownPins()
    {
        for (var i = 0; i < m_safetyPin.Count; i++)
        {
            _pinController.SetPin(i, false);
        }
    }


    private void OnDisable()
    {
        HardDisableAllKnownPins();

        _pinController.OnPinSetEvent -= StartTimer;

        StopAllCoroutines();
    }
}