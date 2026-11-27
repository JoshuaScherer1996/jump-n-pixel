using System;
using UnityEngine;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class LevelRun : MonoBehaviour
{
    [SerializeField, Min(1f)] private float timeLimit = 180f;
    [SerializeField] private PlayerHealth2D playerHealth;

    private int displayedSeconds;

    public float RemainingSeconds { get; private set; }
    public int DisplayedSeconds => Mathf.CeilToInt(RemainingSeconds);
    public event Action<int> TimeChanged;

    private void Awake()
    {
        RemainingSeconds = timeLimit;
        displayedSeconds = DisplayedSeconds;
    }

    private void Update()
    {
        if (Time.timeScale == 0f || playerHealth.CurrentHealth == 0)
            return;

        RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
        if (displayedSeconds != DisplayedSeconds)
        {
            displayedSeconds = DisplayedSeconds;
            TimeChanged?.Invoke(displayedSeconds);
        }

        if (RemainingSeconds == 0f)
            playerHealth.Die();
    }
}
