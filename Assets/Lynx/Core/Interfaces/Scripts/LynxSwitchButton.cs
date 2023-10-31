//   ==============================================================================
//   | Lynx Interfaces (2023)                                                     |
//   |======================================                                      |
//   | LynxSwitchButton Script                                                    |
//   | Script to set a UI element as Switch Button.                               |
//   ==============================================================================

using System.Collections;using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lynx.UI
{
    public class LynxSwitchButton : Button
    {
        #region INSPECTOR VARIABLES

        // Button Parameters
        [SerializeField] private Graphic[] m_secondaryTargetGraphic;
        [SerializeField] private UnityEvent OnPress;
        [SerializeField] private UnityEvent OnUnpress;
        [SerializeField] public UnityEvent OnToggle;
        [SerializeField] public UnityEvent OnUntoggle;
        [SerializeField] private bool m_disableSelectState = true;

        // Switch Button Parameters
        [SerializeField] private Transform m_handle = null;
        [SerializeField] private float m_lerpTime= 0.33f;
        [SerializeField] private ButtonAnimation m_animation = new ButtonAnimation();

        //theme button 
        [SerializeField] private bool useTheme = false;
        #endregion

        #region PRIVATE VARIABLES

        private bool m_isRunning = false; // Avoid multiple press or unpress making the object in unstable state.
        private bool m_isCurrentlyPressed = false; // Status of the current object.
        private bool m_isToggle = false; // Status of the button.
        private bool m_isInteractable = true;


        private Vector3 offHandlePosition;
        private Vector3 onHandlePosition;
        #endregion

        #region UNITY API



        // OnEnable is called when the object becomes enabled and active.
        protected override void OnEnable()
        {
            offHandlePosition = m_handle.localPosition;
            onHandlePosition = new Vector3(0,0,0) - offHandlePosition;

            base.OnEnable();

            StartCoroutine(WaitCoroutine(0.25f, ResetInteractable));

            if (useTheme && LynxThemeManager.Instance)
            {
                LynxThemeManager.Instance.ThemeUpdateEvent += this.SetThemeColors;
                SetThemeColors();
            }

            if (m_isToggle)
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                base.OnPointerDown(eventData);
                base.OnDeselect(eventData);
            }
        }

        protected override void OnDisable()
        {
            interactable = false;
            ButtonAnimationMethods.ResetAnimation(m_animation, this.transform);
            base.OnDisable();
            //if (useTheme)
            //    LynxThemeManager.Instance.ThemeUpdateEvent -= SetThemeColors;
            if (useTheme && LynxThemeManager.Instance)
            {
                LynxThemeManager.Instance.ThemeUpdateEvent -= this.SetThemeColors;
                SetThemeColors();
            }
        }

        // OnSelect is called when the selectable UI object is selected.
        public override void OnSelect(BaseEventData eventData)
        {
            // State Select can affect the expected behaviour of the button.
            // It is natively deactivated on our buttons.
            // But can be reactivated by unchecking disableSelectState

            if (m_disableSelectState)
            {
                base.OnDeselect(eventData);
            }
            else
            {
                base.OnSelect(eventData);
            }
        }

        // OnPointerDown is called when the mouse is clicked over this selectable UI object.
        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable()) return;

            base.OnPointerDown(eventData);

            if (!m_isRunning && !m_isCurrentlyPressed)
            {
                m_isRunning = true;
                StartCoroutine(ButtonAnimationMethods.PressingAnimationCoroutine(m_animation, this.transform, CallbackStopRunning));
                m_isCurrentlyPressed = true;
            }
            if (LynxThemeManager.Instance.currentTheme.CallAudioOnPress(out AudioClip clip) && useTheme)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
            }
        }

        // OnPointerUp is called when the mouse click on this selectable UI object is released.
        public override void OnPointerUp(PointerEventData eventData)
        {
            if (!IsInteractable()) return;

            if (m_isCurrentlyPressed)
            {
                m_isRunning = true;
                StartCoroutine(ButtonAnimationMethods.UnpressingAnimationCoroutine(m_animation, this.transform, CallbackStopRunning));
                m_isCurrentlyPressed = false;
            }
            if (m_isToggle)
            {
                base.OnPointerUp(eventData);
                m_isToggle = false;
                StartCoroutine(ToggleAnimationCoroutine());
                OnUntoggle.Invoke();
            }
            else
            {
                base.OnPointerDown(eventData);
                m_isToggle = true;
                StartCoroutine(ToggleAnimationCoroutine());
                OnToggle.Invoke();
            }
            if (LynxThemeManager.Instance.currentTheme.CallOnAudioUnpress(out AudioClip clip) && useTheme)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
            }
        }


        //DoStateTranistion is called on every state change to manage graphic modification
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if (transition != Transition.ColorTint) return;
            if (m_secondaryTargetGraphic == null) return;

            for (int i = 0; i < m_secondaryTargetGraphic.Length; ++i)
            {
                Color tintColor;
                switch (state)
                {
                    case SelectionState.Normal:
                        tintColor = colors.normalColor;
                        break;
                    case SelectionState.Highlighted:
                        tintColor = colors.highlightedColor;
                        break;
                    case SelectionState.Pressed:
                        tintColor = colors.pressedColor;
                        break;
                    case SelectionState.Selected:
                        tintColor = colors.selectedColor;
                        break;
                    case SelectionState.Disabled:
                        tintColor = colors.disabledColor;
                        break;
                    default:
                        tintColor = Color.black;
                        break;
                }
                m_secondaryTargetGraphic[i].CrossFadeColor(tintColor, instant ? 0f : colors.fadeDuration, true, true);
            }
        }
        #endregion

        #region PRIVATE METHODS

        /// <summary>
        /// Call this coroutine to waiting time.
        /// </summary>
        /// <param name="waitingTime">Time to wait.</param>
        /// <param name="callback">Function to call at the end.</param>
        /// <returns></returns>
        public static IEnumerator WaitCoroutine(float waitingTime, Action<bool> callback)
        {
            yield return new WaitForSeconds(waitingTime);
            callback(false);
        }

        /// <summary>
        /// Call this function to update interactable state of the button.
        /// </summary>
        /// <param name="boolean"></param>
        private void ResetInteractable(bool boolean)
        {
            interactable = m_isInteractable;
        }

        /// <summary>
        /// CallbackStopRunning is called when a button animation coroutine is complete.
        /// </summary>
        /// <param name="state">True to call OnUnpress, false to call OnPress.</param>
        private void CallbackStopRunning(bool state)
        {
            m_isRunning = false;

            if (state)
            {
                OnUnpress.Invoke();
            }
            else
            {
                OnPress.Invoke();
            }
        }

        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Set the state of the toggle, visualy and start coresponding event if state change
        /// </summary>
        /// <param name="state">Is toggle activated</param>
        public void IsToggle(bool state)
        {
            if (m_isToggle != state)
            {
                if (state)
                    OnToggle.Invoke();
                else
                    OnUntoggle.Invoke();
            }
            m_isToggle = state;
        }

        /// <summary>
        /// Set the state of the toggle
        /// </summary>
        /// <param name="state">Is toggle activated</param>
        /// <param name="launchEvent">Should start switch event</param>
        public void IsToggle(bool state, bool launchEvent)
        {
            if (launchEvent)
            {
                if (state)
                    OnToggle.Invoke();
                else
                    OnUntoggle.Invoke();
            }
            m_isToggle = state;
        }

        #endregion

        #region ANIMATION COROUTINES
        /// <summary>
        /// Start this coroutine to activate the switch toggle animation.
        /// </summary>
        private IEnumerator ToggleAnimationCoroutine()
        {
            Vector3 targetPos = m_isToggle ? onHandlePosition : offHandlePosition;
            Vector3 startPos = m_handle.localPosition;
            for(float i = 0; i < 1; i+= Time.deltaTime / m_lerpTime)
            {
                i = Mathf.Min(i, 1);
                m_handle.localPosition = Vector3.Lerp(startPos, targetPos, LynxMath.Ease(i,LynxMath.easingType.CubicInOut));
                yield return new WaitForEndOfFrame();
            }

        }

        #endregion

        #region THEME MANAGING

        /// <summary>
        /// change the colorblock of a button to match the selected theme
        /// </summary>
        public void SetThemeColors()
        {
            if (LynxThemeManager.Instance == null)
                return;
            colors = LynxThemeManager.Instance.currentTheme.selectableColors;
        }

        /// <summary>
        /// Define if this element should use the theme manager
        /// </summary>
        /// <param name="enable">True to use theme manager</param>
        public void SetUseTheme(bool enable = true)
        {
            useTheme = enable;
        }

        /// <summary>
        /// Check if current element is using theme manager.
        /// </summary>
        /// <returns>True if this element use theme manager.</returns>
        public bool IsUsingTheme()
        {
            return useTheme;
        }
        #endregion
    }
}