using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client
{
    /// <summary>
    /// `BL-364` — a MOUSE hover: the pointer entering the widget calls <see cref="OnEnter"/>, leaving calls
    /// <see cref="OnExit"/>. The desktop half of a tap-to-open popup (owner: *"hover over buffs to show/hide the
    /// popup"*). Add it only where a mouse exists (<see cref="Wanted"/>): on a phone a touch also raises enter/exit,
    /// and a card that flashes up under every tap and vanishes on release would fight the tap that opens it.
    /// </summary>
    public class HoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action OnEnter;
        public Action OnExit;

        /// <summary>True on Windows, the editor and a DESKTOP browser — false on Android and a phone's browser.</summary>
        public static bool Wanted => !Application.isMobilePlatform;

        public void OnPointerEnter(PointerEventData eventData) => OnEnter?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => OnExit?.Invoke();

        /// <summary>Hidden or destroyed under the pointer (a buff expired while hovered) raises no exit — do it here.</summary>
        private void OnDisable() => OnExit?.Invoke();
    }
}
