using System.Collections;
using _Project.Develop.UI.Evidence;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using PrimeTween;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace _Project.Develop.UI
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private RectTransform cover;
        [SerializeField] private RectTransform spread;
        [SerializeField] private CanvasGroup pages;
        [SerializeField] private Button play;
        [SerializeField] private Image background;
        [SerializeField] private Shader backdropShader;
        [SerializeField] private Sprite starSprite;
        [SerializeField] private float openingDuration = 1.5f;
        [SerializeField] private NotebookPageCurl outside;
        [SerializeField] private NotebookPageCurl inside;
        [SerializeField, Range(0f, 2f)] private float starIntensity = .85f;
        [SerializeField] private UIEffects coverEffects;
        [SerializeField, Min(.01f)] private float backdropFadeDuration = .95f;
        private static readonly int IntroProgress = Shader.PropertyToID("_IntroProgress");
        private Tween backdropFade;
        private Material backdrop;
        private bool opening;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            backdrop = new Material(backdropShader);
            var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(starSprite);
            backdrop.SetTexture("_StarTex", starSprite.texture);
            backdrop.SetVector("_StarUV", uv);
            backdrop.SetFloat("_StarAspect", starSprite.rect.width / starSprite.rect.height);
            background.material = backdrop;
            play.onClick.AddListener(Open);
        }

        private void Start() => FadeInBackdrop();

        private void FadeInBackdrop()
        {
            backdropFade = Tween.Custom(backdrop, 0f, 1f, backdropFadeDuration,
                (material, progress) => material.SetFloat(IntroProgress, progress),
                Ease.InOutSine, useUnscaledTime: true);
        }

        private void Update()
        {
            backdrop.SetFloat("_Clock", Time.unscaledTime);
            backdrop.SetFloat("_StarIntensity", starIntensity);
            float height = background.rectTransform.rect.height;
            if (height > 0)
                backdrop.SetVector("_BookSize", new Vector4(cover.rect.width / height, cover.rect.height / height, 0, 0));
            if (!opening && EventSystem.current && !EventSystem.current.currentSelectedGameObject)
                EventSystem.current.SetSelectedGameObject(cover.gameObject);
        }

        private void Open()
        {
            if (opening) return;
            opening = true;
            coverEffects.enabled = false;
            Tween.StopAll(cover);
            StartCoroutine(Reveal());
        }

        private IEnumerator Reveal()
        {
            spread.gameObject.SetActive(true);
            Vector2 spine = cover.anchoredPosition;
            Vector2 center = spread.anchoredPosition;
            Vector3 pressedScale = cover.localScale;
            Quaternion hoverRotation = cover.localRotation;
            float zoom = 0f;
            float elapsed = 0;
            while (elapsed < openingDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / openingDuration);
                float turn = Mathf.Min(1, t * 1.45f);
                spread.anchoredPosition = Vector2.Lerp(spine, center, turn);
                cover.anchoredPosition = spread.anchoredPosition;
                float angle = Mathf.PI * turn;
                float width = Mathf.Cos(angle);
                float curl = Mathf.Sin(angle);
                bool frontVisible = width > 0f;
                cover.gameObject.SetActive(frontVisible);
                inside.gameObject.SetActive(!frontVisible);
                if (frontVisible)
                {
                    float settle = Mathf.SmoothStep(0f, 1f, turn * 4f);
                    cover.localRotation = Quaternion.Slerp(hoverRotation, Quaternion.identity, settle);
                    cover.localScale = Vector3.Scale(Vector3.Lerp(pressedScale, Vector3.one, settle), new Vector3(width, 1f, 1f));
                    outside.SetBend(curl, 0f, .4f);
                }
                else
                {
                    inside.transform.localScale = new Vector3(-width, 1f, 1f);
                    inside.SetBend(curl, 1f, .4f);
                }
                backdrop.SetFloat("_Focus", turn);
                if (turn >= .9f)
                    AdvanceZoom(spread.anchoredPosition, ref zoom);
                yield return null;
                if (turn >= 1f) break;
            }
            cover.gameObject.SetActive(false);
            float reveal = 0f;
            while (reveal < 1f)
            {
                float step = Time.unscaledDeltaTime / .55f;
                AdvanceZoom(center, ref zoom);

                if (SceneManager.GetSceneByName("Game").isLoaded)
                {
                    reveal = Mathf.MoveTowards(reveal, 1f, step);
                    float fade = Mathf.SmoothStep(0f, 1f, reveal);
                    backdrop.SetFloat("_Reveal", fade);
                    pages.alpha = 1f - fade;
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        private void AdvanceZoom(Vector2 origin, ref float progress)
        {
            progress = Mathf.MoveTowards(progress, 1f, Time.unscaledDeltaTime / .55f);
            float ease = Mathf.SmoothStep(0f, 1f, progress);
            spread.localScale = Vector3.one * (1 + 2 * ease);
            spread.anchoredPosition = origin + Vector2.left * (spread.rect.width * .75f * ease);
        }

        private void OnDestroy()
        {
            backdropFade.Stop();
            Destroy(backdrop);
        }
    }
}
