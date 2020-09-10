using System;
using System.Collections.Generic;

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using UnityUseful.CorotineInstructionsWrappers;
using UnityUseful.Misc;

using Sirenix.OdinInspector;

[DefaultExecutionOrder(-11000)]
[RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
public class Logger : MonoBehaviour
{
    public List<LogEntry> m_logs;
    [SerializeField] List<LogType> m_allowed_logs;

    [Header("Prefs:")]
    [SerializeField] GameObject log_pref;
    [SerializeField] GameObject func_btn_pref;

    [Header("UI:")]
    [SerializeField] ScrollRect m_scroll_view;
    [Space]
    [SerializeField] TextMeshProUGUI m_stack_text;
    [SerializeField] List<Toggle> m_sort_toggles;
    [Space]
    [SerializeField] Canvas m_canvas;
    [SerializeField] CanvasScaler m_canvas_scaler;

    [Header("OtherPanels:")]
    [SerializeField] List<GameObject> m_main_scroll_contents;

    [Header("Debug functions")]
    [SerializeField] List<string> m_funcs;
    [Space]
    [SerializeField] Transform m_func_parent;

    [Header("Buttons:")]
    [SerializeField] Toggle m_change_state;
    [SerializeField] FancyToggle m_f_toggle;
    [Space]
    [SerializeField] Button m_clear;
    [SerializeField] Button m_close;
    [Space]
    [SerializeField] bool m_stop_receiving;
    [SerializeField] bool m_block_change;
    [SerializeField] bool m_allow_console;

    #region Properties
    public bool StopReceiving { get => m_stop_receiving; set => m_stop_receiving = value; }
    public bool AllowConsole { get => m_allow_console; set => m_allow_console = value; }
    public List<string> Funcs { get => m_funcs; } 
    #endregion

    void Awake()
    {
        DontDestroyOnLoad(this);

        m_canvas_scaler = GetComponent<CanvasScaler>();

        #region SortLogsListeners
        for (var i = 0; i < m_sort_toggles.Count; i++)
        {
            var index = i;
            var log_type = (LogType)index;
            var current = m_sort_toggles?[i];

            if (current)
            {
                current.isOn = m_allowed_logs.Contains(log_type);
                current.onValueChanged.AddListener(state =>
                {
                    SortText(state, log_type);
                });
            }
        }
        #endregion

        #region DragEventOfStateToggle
        var drag_event = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Drag
        };

        var rect = m_change_state.GetComponent<RectTransform>();
        drag_event.callback.AddListener(data => { rect.anchoredPosition += ((PointerEventData)data).delta; });
        m_change_state.GetComponent<EventTrigger>().triggers.Add(drag_event);
        #endregion

        #region PageToggleInit
        var bg_transform = m_change_state.targetGraphic.transform;
        m_change_state.onValueChanged.AddListener(state =>
        {
            var target_angle = Quaternion.Euler(0, 0, state ? 90 : -90);
            this.ProgressTimerV(0.25f, (prog, delta) =>
             {
                 bg_transform.rotation = Quaternion.Lerp(bg_transform.rotation, target_angle, prog);
             });
            m_canvas.enabled = state;
        });

        m_f_toggle.OnValueChangedEndAnim += state =>
        {
            //ConditionalLogger.Log($"<b>Logger.OnValueChangedEndAnim</b> {state}");
            var target = m_main_scroll_contents[state];

            m_scroll_view.content.gameObject.SetActive(false);
            m_scroll_view.content = target.GetComponent<RectTransform>();

            target.SetActive(true);
        };

        m_main_scroll_contents.ForEach(x => x.SetActive(false));
        m_main_scroll_contents[0].SetActive(true);
        #endregion

        m_clear.onClick.AddListener(() =>
        {
            m_main_scroll_contents[0].transform.DestroyChildren();
            m_logs.Clear();
        });
        m_close.onClick.AddListener(() =>
        {
            m_stack_text.transform.parent.gameObject.SetActive(false);
        });

        OnRectTransformDimensionsChange();
    }
    void Start()
    {
        foreach (var func_name in m_funcs)
        {
            var btn = Instantiate(func_btn_pref, m_func_parent);

            var func_btn = btn.GetComponent<Button>();
            func_btn.onClick.AddListener(() => gameObject.SendMessage(func_name));

            var child_text = btn.GetComponentInChildren<TextMeshProUGUI>();
            child_text.text = func_name;

            btn.SetActive(true);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ChangeDebuggerState();
#endif
    }
    void OnEnable()
    {
        Application.logMessageReceived += AddLog;
    }
    void OnDisable()
    {
        Application.logMessageReceived -= AddLog;
    }

    [ContextMenu("Debuger state")]
    [Button]
    void ChangeDebuggerState()
    {
        if (AllowConsole && !m_block_change)
        {
            m_block_change = true;
            m_change_state.gameObject.SetActive(!m_change_state.gameObject.activeSelf);
            this.TimerV(0.5f, () => m_block_change = false);
        }
    }

    #region ConsoleLogs

    void AddLog(string condition, string stackTrace, LogType type)
    {
        if (StopReceiving || !m_allowed_logs.Contains(type))
        {
            return;
        }
        var new_item = Instantiate(log_pref, m_main_scroll_contents[0].transform);
        new_item.name = "Log " + m_logs.Count;
        new_item.isStatic = true;
        var log_entry = new LogEntry() { LogType = type, Condition = condition, StackTrace = stackTrace };
        m_logs.Add(log_entry);

        new_item.GetComponent<TextMeshProUGUI>().text = log_entry.ToString();
        new_item.GetComponent<Button>().onClick.AddListener(() =>
        {
            m_stack_text.text = log_entry.StackTrace;
            m_stack_text.transform.parent.gameObject.SetActive(true);
        });
        new_item.SetActive(true);
    }
    public void SortText(bool state, LogType type)
    {
        if (state)
        {
            if (!m_allowed_logs.Contains(type))
            {
                m_allowed_logs.Add(type);
            }
        }
        else
        {
            if (m_allowed_logs.Contains(type))
            {
                m_allowed_logs.Remove(type);
            }
        }
        SortLogs();
    }

    void Update()
    {
        if (Input.touchCount == 3)
        {
            ChangeDebuggerState();
        }
    }

    public void SortLogs()
    {
        var parent = m_main_scroll_contents[0].transform;

        for (var i = 0; i < m_logs.Count; i++)
        {
            var current_entry = m_logs[i];
            var current_go = parent.GetChild(i).gameObject;
            var active = m_allowed_logs.Contains(current_entry.LogType);

            current_go.SetActive(active);
        }
    }

    [Serializable]
    public class LogEntry
    {
        public LogType LogType;
        public string StackTrace;
        public string Condition;

        public override string ToString()
        {
            var color = "";

            switch (LogType)
            {
                case LogType.Exception: color = "red"; break;
                case LogType.Warning: color = "yellow"; break;
                case LogType.Error: color = "red"; break;

                default:
                    color = "white";
                    break;
            }
            return $"<color={color}>({LogType})</color>, {Condition}";
        }
    }
    #endregion

    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }

    void OnRectTransformDimensionsChange()
    {
        if (m_canvas_scaler)
        {
            switch (Screen.orientation)
            {
                case ScreenOrientation.Portrait: m_canvas_scaler.matchWidthOrHeight = 1f; break;
                case ScreenOrientation.Landscape: m_canvas_scaler.matchWidthOrHeight = 0f; break;

                default: break;
            }
        }
    }
}
