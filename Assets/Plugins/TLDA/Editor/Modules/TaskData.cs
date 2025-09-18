#nullable enable
using UnityEngine;

namespace LivingDevAgent.Editor.Modules
	{
	// ⚠ Intent ⚠ - @jmeyer1980
	// This class is for Taskmaster use, it is not intended for manual use.
	// This is why the CreateAssetMenu attribute is commented out.
	// While the TimeCardSO is used to track time spent on tasks, the TaskSO
	// is used to define and manage the tasks themselves. As an example, these tasks
	// are what I would consider to be billable: Time cards are internal. Tasks are external.
	//                          💎 Diamonds are forever 💎
	// [CreateAssetMenu(fileName = "TaskData", menuName = "Scriptable Objects/TaskData")]
	public class TaskData : ScriptableObject
		{
		// ⚠ Intent ⚠ - @jmeyer1980
		// This class represents a task that can be tracked and managed by Taskmaster.
		// It contains a name, description, and status for the task.
		// It also contains a reference to a TimeCardData, which is used to track time spent on the task.
		[HideInInspector] public string TaskName = string.Empty;
		[HideInInspector] public string taskDescription = string.Empty;
		[HideInInspector] public bool isCompleted;
		[HideInInspector] public TimeCardData timeCard = null!; // set during CreateTask
																// Additional properties and methods can be added as needed to manage tasks.
																// Unlike TimeCardData, the TaskData is intended to be accessed and manipulated,
																// not directly, but via Taskmaster API calls. Taskmaster being the... master.
																// This means we're not going to be parsing it directly in this class.
																// Taskmaster reports to the faculty directly, not the other way around.😉
		[HideInInspector] public bool isFinished;
		[HideInInspector] public bool isCanceled;
		[HideInInspector] public System.DateTime createdAt;
		[HideInInspector] public System.DateTime completedAt;
		[HideInInspector] public System.DateTime canceledAt;
		[HideInInspector] public string assignedTo = string.Empty;
		[HideInInspector] public int priorityLevel; // 1 (highest) to 5 (lowest)
		[HideInInspector] public string[] tags = System.Array.Empty<string>(); // e.g., "bug"
		[HideInInspector] public string[] comments = System.Array.Empty<string>(); // e.g., "Started working on this task."
		[HideInInspector] public string[] attachments = System.Array.Empty<string>(); // e.g., "screenshot.png"
		[HideInInspector] public string[] subtasks = System.Array.Empty<string>(); // e.g., "Design UI"
		[HideInInspector] public string[] relatedRefs = System.Array.Empty<string>();
		[HideInInspector] public string[] dependencies = System.Array.Empty<string>();
		[HideInInspector] public string[] blockers = System.Array.Empty<string>();
		[HideInInspector] public string[] watchers = System.Array.Empty<string>();
		[HideInInspector] public string[] history = System.Array.Empty<string>(); // seed history on creation
		[HideInInspector] public string[] customFields = System.Array.Empty<string>();
		[HideInInspector] public string[] auditLogs = System.Array.Empty<string>();
		[HideInInspector] public string[] notifications = System.Array.Empty<string>();
		[HideInInspector] public string[] reminders = System.Array.Empty<string>();
		[HideInInspector] public string[] timeEstimates = System.Array.Empty<string>();
		[HideInInspector] public string[] timeSpent = System.Array.Empty<string>();
		[HideInInspector] public string[] billingCodes = System.Array.Empty<string>();

		// Create a new TaskData instance
		public static TaskData CreateTask(string name, string description, string assignedUser, int priority)
			{
			TaskData newTask = CreateInstance<TaskData>();
			newTask.TaskName = name; // Fixed: Use proper field name
			newTask.taskDescription = description;
			newTask.isCompleted = false;
			newTask.isFinished = false;
			newTask.isCanceled = false;
			newTask.createdAt = System.DateTime.Now;
			newTask.assignedTo = assignedUser;
			newTask.priorityLevel = priority;
			newTask.history = new string[] { $"Task created at {newTask.createdAt}" }; // all other arrays already sentinel-initialized
																					   // Initialize the TimeCardData for this task
			TimeCardData timeCardInstance = CreateInstance<TimeCardData>();
			timeCardInstance.StartTimeCard(newTask);
			newTask.timeCard = timeCardInstance;
			return newTask;
			}

		// Mark the task as completed
		public void CompleteTask()
			{
			if (!isCompleted && !isCanceled)
				{
				isCompleted = true;
				isFinished = true;
				completedAt = System.DateTime.Now;
				timeCard.EndTimeCard();
				history = AppendToArray(history, $"Task completed at {completedAt}");
				}
			else
				{
				Debug.LogWarning("TaskData: Attempted to complete a task that is already completed or canceled.");
				}
			}

		// Cancel the task
		public void CancelTask()
			{
			if (!isCompleted && !isCanceled)
				{
				isCanceled = true;
				isFinished = true;
				canceledAt = System.DateTime.Now;
				history = AppendToArray(history, $"Task canceled at {canceledAt}");
				}
			else
				{
				Debug.LogWarning("TaskData: Attempted to cancel a task that is already completed or canceled.");
				}
			}

		// Helper method to append a string to a string array
		private string[] AppendToArray(string[] array, string newItem)
			{
			string[] newArray = new string[array.Length + 1];
			array.CopyTo(newArray, 0);
			newArray[array.Length] = newItem;
			return newArray;
			}
		}
	}
