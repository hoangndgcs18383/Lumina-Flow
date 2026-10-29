using UnityEngine;
#if LUMINA_DOTWEEN
using DG.Tweening;
#endif

namespace LuminaFlow
{
    public sealed class PopupAnimation : MonoBehaviour
    {
        private CanvasGroup group;
        private float age;
        private void OnEnable()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            transform.localScale = Vector3.one * .94f;
            age = 0;
#if LUMINA_DOTWEEN
            group.DOFade(1, .2f).SetUpdate(true);
            transform.DOScale(1, .25f).SetEase(Ease.OutCubic).SetUpdate(true);
#endif
        }
        private void Update()
        {
#if !LUMINA_DOTWEEN
            age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(age / .23f);
            float eased = 1 - Mathf.Pow(1 - t, 3);
            group.alpha = t;
            transform.localScale = Vector3.one * Mathf.Lerp(.94f, 1, eased);
#endif
        }
#if LUMINA_DOTWEEN
        private void OnDestroy() { transform.DOKill(); if (group != null) group.DOKill(); }
#endif
    }
}
