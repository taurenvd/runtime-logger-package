using TMPro;
using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections;
using System.Collections.Generic;

using UnityUseful.CorotineInstructionsWrappers;
using UnityUseful.IEnumeratorUtils;

public class FancyToggle : MonoBehaviour
{
    [Header("Parent:")]
    [SerializeField] RectTransform m_parent_rect;
    [SerializeField] Image m_parent_image;
    [SerializeField] Button m_button;

    [Header("Moving object props:")]
    [SerializeField] RectTransform m_moving_rect;
    [SerializeField] Image m_moving_i;

    [Header("Settings:")]
    [SerializeField, Tooltip("Base label color")] Color m_not_active_color = Color.white;
    [SerializeField, Tooltip("Background panel multiply color")] Color m_multiply_color = new Color(1, 1, 1, .3f);
    [Space]
    [SerializeField] float m_transition_time = .25f;
    [SerializeField] RectOffset m_offset_rect;
    [Space]
    [SerializeField] int m_current_index;
    [Space]
    [SerializeField] List<TogglePositions> m_positions;
    [SerializeField] TMP_FontAsset m_font;

    [SerializeField] bool m_override_material;
    [SerializeField] bool m_change_color;
    [SerializeField] bool m_initialized;
    
    [SerializeField] Material m_font_material;

    #region Properties

    public bool IsOn
    {
        get => m_current_index == 0;
        set
        {
            CurrentIndex = value ? 1 : 0;
        }
    }

    public int CurrentIndex
    {
        get => m_current_index;
        set
        {
            m_current_index = (int)Mathf.Repeat(value, m_positions.Count);
            OnValueChangedStartAnim?.Invoke(value);
            ChangeState();
        }
    }

    public Color Color
    {
        set
        {
            ParentColor = value;
            SliderColor = value;
            for (var i = 0; i < m_positions.Count; i++)
            {
                if (i != CurrentIndex)
                {
                    m_positions[i].label.color = value;
                }
            }
        }
    }
    public Color ParentColor
    {
        get => m_parent_image.color;
        set => m_parent_image.color = value;
    }
    public Color SliderColor
    {
        get => m_moving_i.color;
        set => m_moving_i.color = value;
    }
    public Color ActiveColor
    {
        set
        {
            m_positions[CurrentIndex].label.color = value;
        }
    }
    public Color OthersColor
    {
        set
        {
            for (var i = 0; i < m_positions.Count; i++)
            {
                if (i!=CurrentIndex)
                {
                    m_positions[i].label.color = value;
                }
            }
        }
    }

    public List<TogglePositions> Positions { get => m_positions; }
    public bool Initialized { get => m_initialized; private set => m_initialized = value; }

    #endregion

    public event Action<int> OnValueChangedStartAnim;
    public event Action<int> OnValueChangedEndAnim;

    void Awake()
    {
        var horizontal = m_parent_rect.gameObject.AddComponent<HorizontalLayoutGroup>();

        horizontal.childAlignment = TextAnchor.MiddleCenter;
        horizontal.padding = m_offset_rect;
        horizontal.spacing = 25;
        horizontal.childControlHeight = true;
        horizontal.childControlWidth = true;
        horizontal.childForceExpandHeight = true;
        horizontal.childForceExpandWidth = true;

        m_button.onClick.AddListener(() =>
        {
            ++CurrentIndex;
        });

        for (var i = 0; i < m_positions.Count; i++)
        {
            var label = m_positions[i].label ?? CreateLabel(i);

            label.text = m_positions[i].text;
            label.color = i != m_current_index ? m_positions[m_current_index].color : m_not_active_color;
        }
    }
    void OnDestroy()
    {
        this.StopAllCoroutinesLogged();
    }
    TextMeshProUGUI CreateLabel(int i)
    {
        var new_label = new GameObject($"{i} text");
        var rect = new_label.AddComponent<RectTransform>();
        var label = new_label.AddComponent<TextMeshProUGUI>();
        var layout = new_label.AddComponent<LayoutElement>();
        var width = m_parent_rect.sizeDelta.x / m_positions.Count;

        layout.preferredWidth = 1000;

        label.font = m_font;
        if (m_override_material)
        {
            label.font.material = m_font_material;
        }        
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Center;
        label.text = m_positions[i].text;
        label.fontSizeMax = 35;
        label.enableAutoSizing = true;

        new_label.transform.SetParent(m_parent_rect);
        rect.anchoredPosition = Vector3.zero;

        m_positions[i].label = label;

        return label;
    }

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        m_parent_image.color = m_positions[0].color * m_multiply_color;
        m_moving_rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_parent_rect.rect.width / m_positions.Count);
        m_moving_rect.transform.position = m_positions[0].label.transform.position;
        m_moving_i.color = m_positions[0].color;

        m_initialized = true;
    }

    void ChangeState()
    {
        //ConditionalLogger.Log($"<b>FancyToggle.ChangeState</b> :{m_current_index}");
        if (gameObject.activeInHierarchy)
        {

            m_button.interactable = false;

            var start_pos = m_moving_rect.transform.position;
            var dest_pos = m_positions[m_current_index].label.transform.position;
            //var dest_pos = m_parent_rect.anchoredPosition;

            //dest_pos.x += (IsOn ? m_parent_rect.rect.size.x / 4 : -m_parent_rect.rect.size.x / 4);
            foreach (var item in m_positions)
            {
                item.label.text = string.Empty;
            }

            this.ProgressTimerV(m_transition_time, (prog, delta) =>
            {
                m_moving_rect.transform.position = Vector2.Lerp(start_pos, dest_pos, prog);
                if (m_change_color)
                {
                    var color = Color.Lerp(m_moving_i.color, m_positions[m_current_index].color, prog);
                    m_moving_i.color = color;
                    m_parent_image.color = color * m_multiply_color;
                }
            }, finals:() =>
            {
                m_button.interactable = true;
                for (var i = 0; i < m_positions.Count; i++)
                {
                    m_positions[i].label.text = m_positions[i].text;
                    if (m_change_color)
                    {
                        m_positions[i].label.color = i != m_current_index ? m_positions[m_current_index].color : m_not_active_color;
                    }
                    else
                    {
                        if (i == m_current_index)
                        {
                            m_positions[i].label.color = m_not_active_color;
                        }
                    }
                }
                OnValueChangedEndAnim?.Invoke(CurrentIndex);
            });
        }
    }

    [Serializable]
    public class TogglePositions
    {
        public string text;
        public TextMeshProUGUI label;
        public Color color;
    }
}
