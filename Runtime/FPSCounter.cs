using TMPro;
using UnityEngine;

using UnityUseful.AsyncExtensions;

public class FPSCounter : MonoBehaviour
{
    const string fps_format = "{0} fps {1:0.0}ms";

    [SerializeField] float m_interval = 0.33f;
    [SerializeField] float m_delta;
    [Space]
    [SerializeField] bool m_enable;
    [SerializeField] bool m_running;
    [Space]
    [SerializeField] int m_fps;
    [Space]
    [SerializeField] TextMeshProUGUI m_text_target;

    #region Properties
    public bool EnableCounter
    {
        get
        {
            return m_enable;
        }
        set
        {
            m_enable = value;
            if (value && !m_running)
            {
                Counter();
            }
        }
    }
    public int Fps
    {
        get
        {
            return m_fps;
        }
        private set
        {
            if (m_text_target)
            {
                m_text_target.text = string.Format(fps_format, value, m_delta * 1000);
            }
            m_fps = value;
        }
    }

    public float Delta
    {
        get => m_delta;
        set
        {
            Fps = Mathf.FloorToInt(1f / value);
            m_delta = value;
        }
    } 
    #endregion

    async void Counter()
    {
        m_running = true;
        var wait_realtime = new WaitForSecondsRealtime(m_interval);
        var wait_frame_end = new WaitForEndOfFrame();

        while (m_enable)
        {
            await wait_realtime;
            await wait_frame_end;
            Delta = Time.unscaledDeltaTime;
        }
        m_running = false;
    }
}

