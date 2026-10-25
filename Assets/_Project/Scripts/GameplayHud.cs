using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class GameplayHud : MonoBehaviour
{
    [SerializeField] private LevelScore levelScore;
    [SerializeField] private LevelRun levelRun;
    [SerializeField] private PlayerHealth2D playerHealth;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private RectTransform heartsRoot;
    [SerializeField] private UnityEngine.UI.Image heartTemplate;
    [SerializeField] private GameObject timeoutMessage;
    [SerializeField] private UnityEngine.UI.CanvasScaler canvasScaler;
    [SerializeField] private Color shieldColor = new Color(0.65f, 1f, 0.65f, 1f);
    [SerializeField] private Color lowTimeColor = new Color(1f, 0.8f, 0.55f, 1f);

    private readonly List<UnityEngine.UI.Image> hearts = new List<UnityEngine.UI.Image>();
    private int screenWidth;
    private int screenHeight;

    private void Awake()
    {
        hearts.AddRange(heartsRoot.GetComponentsInChildren<UnityEngine.UI.Image>(true));
    }

    private void OnEnable()
    {
        levelScore.ScoreChanged += UpdateScore;
        levelRun.TimeChanged += UpdateTime;
        levelRun.Expired += ShowTimeout;
        playerHealth.HealthChanged += UpdateHealth;
        playerHealth.ShieldChanged += UpdateShield;
        Refresh();
    }

    private void OnDisable()
    {
        levelScore.ScoreChanged -= UpdateScore;
        levelRun.TimeChanged -= UpdateTime;
        levelRun.Expired -= ShowTimeout;
        playerHealth.HealthChanged -= UpdateHealth;
        playerHealth.ShieldChanged -= UpdateShield;
    }

    private void LateUpdate()
    {
        if (screenWidth != Screen.width || screenHeight != Screen.height)
            UpdateScale();
    }

    private void Refresh()
    {
        UpdateScore(levelScore.Score);
        UpdateTime(levelRun.DisplayedSeconds);
        UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaximumHealth);
        timeoutMessage.SetActive(levelRun.IsExpired);
        UpdateScale();
    }

    private void UpdateScale()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        canvasScaler.scaleFactor = Mathf.Max(1f, Mathf.Floor(Mathf.Min(screenWidth / 512f, screenHeight / 288f)));
    }

    private void UpdateScore(int score)
    {
        scoreText.text = score.ToString("D6");
    }

    private void UpdateTime(int seconds)
    {
        timeText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        timeText.color = seconds < 30 ? lowTimeColor : Color.white;
    }

    private void UpdateHealth(int current, int maximum)
    {
        while (hearts.Count < maximum)
        {
            var heart = Instantiate(heartTemplate, heartsRoot);
            heart.name = $"Heart{hearts.Count + 1}";
            hearts.Add(heart);
        }

        for (var i = 0; i < hearts.Count; i++)
        {
            hearts[i].color = playerHealth.HasShield ? shieldColor : Color.white;
            hearts[i].gameObject.SetActive(i < current && i < maximum);
        }
    }

    private void UpdateShield(bool active)
    {
        foreach (var heart in hearts)
            heart.color = active ? shieldColor : Color.white;
    }

    private void ShowTimeout()
    {
        timeoutMessage.SetActive(true);
    }
}
