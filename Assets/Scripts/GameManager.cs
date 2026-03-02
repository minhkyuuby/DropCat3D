using CatDrop3D.Inventory3D;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
	public enum GameState
	{
		Waiting,
		Running,
		Won,
		Lost
	}

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;

	[Header("Timer")]
	[Min(0.1f)]
	[SerializeField] private float totalTimeSeconds = 60f;

	[SerializeField] private InventoryGrid3D grid;

	[SerializeField] private bool autoStartOnEnable = true;

	[Header("Events")]
	[SerializeField] private UnityEvent onWin;
	[SerializeField] private UnityEvent onLose;
	[SerializeField] private UnityEvent<float> onTimeChanged;

	private GameState state = GameState.Waiting;
	private float timeRemaining;

	public GameState State => state;
	public float TimeRemaining => timeRemaining;

	public void RegisterTimeListener(UnityAction<float> listener)
	{
		if (listener == null)
		{
			return;
		}

		onTimeChanged.AddListener(listener);
		listener.Invoke(timeRemaining);
	}

	public void UnregisterTimeListener(UnityAction<float> listener)
	{
		if (listener == null)
		{
			return;
		}

		onTimeChanged.RemoveListener(listener);
	}

	private void OnEnable()
	{
		if (grid == null)
		{
			grid = FindFirstObjectByType<InventoryGrid3D>();
		}
		BallItem3D.BallResolved += HandleBallResolved;
		ResetTimer();
		if (autoStartOnEnable)
		{
			StartGame();
		}
	}

	private void OnDisable()
	{
		BallItem3D.BallResolved -= HandleBallResolved;
	}

	private void Update()
	{
		if (state != GameState.Running)
		{
			return;
		}

		timeRemaining -= Time.deltaTime;
		if (timeRemaining < 0f)
		{
			timeRemaining = 0f;
		}
		onTimeChanged?.Invoke(timeRemaining);

		if (timeRemaining <= 0f)
		{
			Lose();
		}
	}

	public void StartGame()
	{
		if (state == GameState.Running)
		{
			return;
		}

		ResetTimer();
		state = GameState.Running;
		CheckWinCondition();
	}

	public void StopGame()
	{
		if (state != GameState.Running)
		{
			return;
		}

		state = GameState.Waiting;
	}

	public void ResetTimer()
	{
		timeRemaining = totalTimeSeconds;
		onTimeChanged?.Invoke(timeRemaining);
	}

	private void HandleBallResolved(BallItem3D ball)
	{
		if (state != GameState.Running)
		{
			return;
		}

		CheckWinCondition();
	}

	private void CheckWinCondition()
	{
		if (AreAllBallsResolved())
		{
			Win();
		}
	}

	private bool AreAllBallsResolved()
	{
		if (grid == null)
		{
			return true;
		}

		return !grid.HasAnyBalls();
	}

	private void Win()
	{
		if (state == GameState.Won)
		{
			return;
		}

        audioSource.Play();
		state = GameState.Won;
		onWin?.Invoke();
	}

	private void Lose()
	{
		if (state == GameState.Lost)
		{
			return;
		}

		state = GameState.Lost;
		onLose?.Invoke();
	}
}
