namespace Web.Services;

/// <summary>
/// Service for managing toast notifications in the UI.
/// </summary>
public class ToastService
{
	private readonly List<ToastMessage> _toasts = [];

	/// <summary>
	/// Event fired when a new toast is added.
	/// </summary>
	public event Action? OnChange;

	/// <summary>
	/// Gets the current list of active toasts.
	/// </summary>
	public IReadOnlyList<ToastMessage> Toasts => _toasts.AsReadOnly();

	/// <summary>
	/// Shows an info toast notification.
	/// </summary>
	/// <param name="message">The message to display.</param>
	/// <param name="durationMs">Duration in milliseconds (default 5000).</param>
	public void ShowInfo(string message, int durationMs = 5000)
	{
		AddToast(ToastType.Info, message, durationMs);
	}

	/// <summary>
	/// Shows a success toast notification.
	/// </summary>
	/// <param name="message">The message to display.</param>
	/// <param name="durationMs">Duration in milliseconds (default 5000).</param>
	public void ShowSuccess(string message, int durationMs = 5000)
	{
		AddToast(ToastType.Success, message, durationMs);
	}

	/// <summary>
	/// Shows a warning toast notification.
	/// </summary>
	/// <param name="message">The message to display.</param>
	/// <param name="durationMs">Duration in milliseconds (default 5000).</param>
	public void ShowWarning(string message, int durationMs = 5000)
	{
		AddToast(ToastType.Warning, message, durationMs);
	}

	/// <summary>
	/// Shows an error toast notification.
	/// </summary>
	/// <param name="message">The message to display.</param>
	/// <param name="durationMs">Duration in milliseconds (default 5000).</param>
	public void ShowError(string message, int durationMs = 5000)
	{
		AddToast(ToastType.Error, message, durationMs);
	}

	/// <summary>
	/// Removes a toast from the list.
	/// </summary>
	/// <param name="id">The ID of the toast to remove.</param>
	public void RemoveToast(Guid id)
	{
		var toast = _toasts.FirstOrDefault(t => t.Id == id);
		if (toast != null)
		{
			_toasts.Remove(toast);
			OnChange?.Invoke();
		}
	}

	private void AddToast(ToastType type, string message, int durationMs)
	{
		var toast = new ToastMessage
		{
			Id = Guid.NewGuid(),
			Type = type,
			Message = message,
			DurationMs = durationMs,
			CreatedAt = DateTime.UtcNow
		};

		_toasts.Add(toast);
		OnChange?.Invoke();
	}
}

/// <summary>
/// Represents a toast notification message.
/// </summary>
public class ToastMessage
{
	public Guid Id { get; init; }
	public ToastType Type { get; init; }
	public required string Message { get; init; }
	public int DurationMs { get; init; }
	public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Types of toast notifications.
/// </summary>
public enum ToastType
{
	Info,
	Success,
	Warning,
	Error
}
